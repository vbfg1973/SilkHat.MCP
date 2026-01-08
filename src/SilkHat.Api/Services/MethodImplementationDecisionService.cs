using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Infrastructure;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Services;

public sealed class MethodImplementationDecisionService : IMethodImplementationDecisionService
{
    private const string DecisionType = "InterfaceImplementation";
    private readonly SilkHatDbContext _dbContext;

    public MethodImplementationDecisionService(SilkHatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MethodImplementationResolution> ResolveAsync(
        CodeRepositoryWorkspace workspace,
        CodeSolutionWorkspace solution,
        Guid repositoryConfigId,
        string solutionId,
        IMethodSymbol interfaceMethod,
        CancellationToken cancellationToken)
    {
        if (interfaceMethod.ContainingType.TypeKind != TypeKind.Interface
            && interfaceMethod.ContainingType.TypeKind != TypeKind.Error
            && interfaceMethod.ContainingType.TypeKind != TypeKind.Unknown)
        {
            return new MethodImplementationResolution(null, null, false, Array.Empty<string>(), Array.Empty<string?>());
        }

        var interfaceTypeName = GetTypeDisplayName(interfaceMethod.ContainingType);
        var interfaceTypeDocId = DocumentationIdUtility.GetDocumentationId(interfaceMethod.ContainingType);
        var interfaceMethodSignature = BuildInterfaceMethodSignature(interfaceMethod);
        var interfaceMetadataName = GetMetadataName(interfaceMethod.ContainingType);
        var interfaceNamespace = interfaceMethod.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var interfaceMethodDocId = DocumentationIdUtility.GetDocumentationId(interfaceMethod);
        var storedDecision = await _dbContext.MethodImplementationDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(decision =>
                decision.RepositoryConfigId == repositoryConfigId
                && decision.SolutionId == solutionId
                && (!string.IsNullOrWhiteSpace(interfaceMethodDocId)
                    ? (decision.InterfaceMethodDocumentationId == interfaceMethodDocId
                        || (decision.InterfaceMethodDocumentationId == null
                            && decision.InterfaceMethodSignature == interfaceMethodSignature))
                    : decision.InterfaceMethodSignature == interfaceMethodSignature),
                cancellationToken);

        if (storedDecision is not null)
        {
            var resolved = ResolveImplementationByDocId(solution, storedDecision.ImplementationMethodDocumentationId)
                ?? ResolveImplementationByName(
                    solution,
                    interfaceMethod,
                    interfaceMetadataName,
                    interfaceTypeName,
                    storedDecision.ImplementationTypeName);
            if (resolved is not null)
            {
                return new MethodImplementationResolution(
                    resolved,
                    new DecisionUsage(storedDecision.Id, DecisionType),
                    false,
                    Array.Empty<string>(),
                    Array.Empty<string?>());
            }
        }

        var candidates = DeduplicateCandidates(ResolveCandidates(
            solution,
            interfaceMethod,
            interfaceMetadataName,
            interfaceTypeName,
            interfaceTypeDocId)).ToList();
        if (candidates.Count == 0)
        {
            candidates = DeduplicateCandidates(
                ResolveCandidatesBySignature(solution, interfaceMethod, interfaceNamespace)).ToList();
        }
        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(interfaceNamespace))
        {
            candidates = DeduplicateCandidates(
                ResolveCandidatesBySignature(solution, interfaceMethod, string.Empty)).ToList();
        }
        if (candidates.Count == 0)
        {
            candidates = DeduplicateCandidates(
                ResolveCandidatesByLooseSignature(solution, interfaceMethod)).ToList();
        }
        if (candidates.Count == 0)
        {
            return new MethodImplementationResolution(null, null, false, Array.Empty<string>(), Array.Empty<string?>());
        }

        if (candidates.Count == 1)
        {
            return new MethodImplementationResolution(
                candidates[0].Method,
                null,
                false,
                new[] { candidates[0].TypeName },
                new[] { candidates[0].MethodDocumentationId });
        }

        var nonTestCandidates = candidates.Where(candidate => !candidate.IsTestProject).ToList();
        if (nonTestCandidates.Count == 1)
        {
            return new MethodImplementationResolution(
                nonTestCandidates[0].Method,
                null,
                false,
                nonTestCandidates.Select(candidate => candidate.TypeName).ToList(),
                nonTestCandidates.Select(candidate => candidate.MethodDocumentationId).ToList());
        }

        return new MethodImplementationResolution(
            null,
            null,
            true,
            nonTestCandidates.Select(candidate => candidate.TypeName).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            nonTestCandidates.Select(candidate => candidate.MethodDocumentationId).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static IMethodSymbol? ResolveImplementationByDocId(
        CodeSolutionWorkspace solution,
        string? implementationMethodDocId)
    {
        if (string.IsNullOrWhiteSpace(implementationMethodDocId))
        {
            return null;
        }

        return DocumentationIdUtility.FindMethodByDocumentationId(solution, implementationMethodDocId);
    }

    private static IMethodSymbol? ResolveImplementationByName(
        CodeSolutionWorkspace solution,
        IMethodSymbol interfaceMethod,
        string interfaceMetadataName,
        string interfaceTypeName,
        string typeName)
    {
        foreach (var compilation in solution.Compilations.Values)
        {
            var interfaceType = compilation.GetTypeByMetadataName(interfaceMetadataName);
            var interfaceMember = interfaceType is null ? null : ResolveInterfaceMember(interfaceType, interfaceMethod);

            foreach (var candidate in EnumerateNamedTypes(compilation.GlobalNamespace))
            {
                var candidateName = GetTypeDisplayName(candidate);
                if (!string.Equals(candidateName, typeName, StringComparison.Ordinal))
                {
                    continue;
                }

                var impl = ResolveImplementationMethod(candidate, interfaceMethod, interfaceMember);
                if (impl is not null)
                {
                    return impl;
                }
            }
        }

        return null;
    }

    private static IEnumerable<ImplementationCandidate> ResolveCandidates(
        CodeSolutionWorkspace solution,
        IMethodSymbol interfaceMethod,
        string interfaceMetadataName,
        string interfaceTypeName,
        string? interfaceTypeDocId)
    {
        var testAssemblyNames = solution.Projects.Values
            .Where(project => IsTestProject(project.Name))
            .Select(project => project.AssemblyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (compilation, root) in EnumerateCandidateRoots(solution, interfaceMethod))
        {
            var interfaceType = compilation?.GetTypeByMetadataName(interfaceMetadataName)
                ?? interfaceMethod.ContainingType;
            var interfaceMember = ResolveInterfaceMember(interfaceType, interfaceMethod);

            foreach (var candidate in EnumerateNamedTypes(root))
            {
                if (candidate.TypeKind == TypeKind.Interface || candidate.IsAbstract)
                {
                    continue;
                }

                if (!ImplementsInterface(candidate, interfaceType, interfaceTypeName, interfaceTypeDocId))
                {
                    continue;
                }

                var method = ResolveImplementationMethod(candidate, interfaceMethod, interfaceMember);
                if (method is null)
                {
                    continue;
                }

                var candidateName = GetTypeDisplayName(candidate);
                var isTestProject = candidate.ContainingAssembly is not null
                    && testAssemblyNames.Contains(candidate.ContainingAssembly.Name);

                yield return new ImplementationCandidate(
                    candidateName,
                    method,
                    DocumentationIdUtility.GetDocumentationId(method),
                    isTestProject);
            }
        }
    }

    private static IEnumerable<ImplementationCandidate> ResolveCandidatesBySignature(
        CodeSolutionWorkspace solution,
        IMethodSymbol interfaceMethod,
        string interfaceNamespace)
    {
        var testAssemblyNames = solution.Projects.Values
            .Where(project => IsTestProject(project.Name))
            .Select(project => project.AssemblyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, root) in EnumerateCandidateRoots(solution, interfaceMethod))
        {
            foreach (var candidate in EnumerateNamedTypes(root))
            {
                if (candidate.TypeKind == TypeKind.Interface || candidate.IsAbstract)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(interfaceNamespace)
                    && !string.Equals(candidate.ContainingNamespace?.ToDisplayString(), interfaceNamespace, StringComparison.Ordinal))
                {
                    continue;
                }

                var method = MatchImplementationBySignature(candidate, interfaceMethod);
                if (method is null)
                {
                    continue;
                }

                var candidateName = GetTypeDisplayName(candidate);
                var isTestProject = candidate.ContainingAssembly is not null
                    && testAssemblyNames.Contains(candidate.ContainingAssembly.Name);

                yield return new ImplementationCandidate(
                    candidateName,
                    method,
                    DocumentationIdUtility.GetDocumentationId(method),
                    isTestProject);
            }
        }
    }

    private static IEnumerable<ImplementationCandidate> ResolveCandidatesByLooseSignature(
        CodeSolutionWorkspace solution,
        IMethodSymbol interfaceMethod)
    {
        var testAssemblyNames = solution.Projects.Values
            .Where(project => IsTestProject(project.Name))
            .Select(project => project.AssemblyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var parameterSignature = interfaceMethod.Parameters
            .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToArray();
        var propertyName = GetPropertyTargetName(interfaceMethod);
        var eventName = GetEventTargetName(interfaceMethod);

        foreach (var (_, root) in EnumerateCandidateRoots(solution, interfaceMethod))
        {
            foreach (var candidate in EnumerateNamedTypes(root))
            {
                if (candidate.TypeKind == TypeKind.Interface || candidate.IsAbstract)
                {
                    continue;
                }

                IMethodSymbol? method = null;
                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    var candidateProperty = candidate.GetMembers(propertyName)
                        .OfType<IPropertySymbol>()
                        .FirstOrDefault();
                    if (candidateProperty is not null)
                    {
                        method = interfaceMethod.Name.StartsWith("set_", StringComparison.Ordinal)
                            || interfaceMethod.MethodKind == MethodKind.PropertySet
                            ? candidateProperty.SetMethod
                            : candidateProperty.GetMethod ?? candidateProperty.SetMethod;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(eventName))
                {
                    var candidateEvent = candidate.GetMembers(eventName)
                        .OfType<IEventSymbol>()
                        .FirstOrDefault();
                    if (candidateEvent is not null)
                    {
                        method = interfaceMethod.Name.StartsWith("remove_", StringComparison.Ordinal)
                            || interfaceMethod.MethodKind == MethodKind.EventRemove
                            ? candidateEvent.RemoveMethod
                            : candidateEvent.AddMethod ?? candidateEvent.RemoveMethod;
                    }
                }
                else
                {
                    method = candidate.GetMembers(interfaceMethod.Name)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(candidateMethod =>
                            candidateMethod.Parameters.Length == parameterSignature.Length
                            && candidateMethod.Parameters
                                .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                                .SequenceEqual(parameterSignature, StringComparer.Ordinal));
                }

                if (method is null)
                {
                    continue;
                }

                var candidateName = GetTypeDisplayName(candidate);
                var isTestProject = candidate.ContainingAssembly is not null
                    && testAssemblyNames.Contains(candidate.ContainingAssembly.Name);

                yield return new ImplementationCandidate(
                    candidateName,
                    method,
                    DocumentationIdUtility.GetDocumentationId(method),
                    isTestProject);
            }
        }
    }

    private static bool ImplementsInterface(
        INamedTypeSymbol candidate,
        INamedTypeSymbol? interfaceType,
        string interfaceTypeName,
        string? interfaceTypeDocId)
    {
        if (interfaceType is not null
            && candidate.AllInterfaces.Contains(interfaceType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(interfaceTypeDocId))
        {
            var matchesDocId = candidate.AllInterfaces.Any(iface =>
                string.Equals(
                    DocumentationIdUtility.GetDocumentationId(iface),
                    interfaceTypeDocId,
                    StringComparison.Ordinal));
            if (matchesDocId)
            {
                return true;
            }
        }

        return candidate.AllInterfaces.Any(iface =>
            string.Equals(GetTypeDisplayName(iface), interfaceTypeName, StringComparison.Ordinal));
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            if (member is INamespaceSymbol ns)
            {
                foreach (var nested in EnumerateNamedTypes(ns))
                {
                    yield return nested;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in type.GetTypeMembers())
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<(Compilation? Compilation, INamespaceSymbol Root)> EnumerateCandidateRoots(
        CodeSolutionWorkspace solution,
        IMethodSymbol interfaceMethod)
    {
        var seenAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        foreach (var compilation in solution.Compilations.Values)
        {
            if (seenAssemblies.Add(compilation.Assembly))
            {
                yield return (compilation, compilation.GlobalNamespace);
            }
        }

        var interfaceAssembly = interfaceMethod.ContainingAssembly;
        if (interfaceAssembly is not null && seenAssemblies.Add(interfaceAssembly))
        {
            yield return (null, interfaceAssembly.GlobalNamespace);
        }
    }

    private static ISymbol GetInterfaceMember(IMethodSymbol interfaceMethod)
    {
        if (interfaceMethod.AssociatedSymbol is not null)
        {
            return interfaceMethod.AssociatedSymbol;
        }

        if (interfaceMethod.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            var propertyName = GetPropertyTargetName(interfaceMethod);
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                var property = interfaceMethod.ContainingType.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();
                if (property is not null)
                {
                    return property;
                }
            }
        }

        if (interfaceMethod.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
        {
            var eventName = GetEventTargetName(interfaceMethod);
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                var @event = interfaceMethod.ContainingType.GetMembers(eventName)
                    .OfType<IEventSymbol>()
                    .FirstOrDefault();
                if (@event is not null)
                {
                    return @event;
                }
            }
        }

        return interfaceMethod;
    }

    private static string? GetAccessorTargetName(string methodName, string prefix)
    {
        return methodName.StartsWith(prefix, StringComparison.Ordinal)
            ? methodName.Substring(prefix.Length)
            : null;
    }

    private static string? GetPropertyTargetName(IMethodSymbol method)
    {
        if (method.MethodKind is not MethodKind.PropertyGet
            && method.MethodKind is not MethodKind.PropertySet)
        {
            return null;
        }

        return GetAccessorTargetName(method.Name, "get_")
            ?? GetAccessorTargetName(method.Name, "set_")
            ?? method.Name;
    }

    private static string? GetEventTargetName(IMethodSymbol method)
    {
        if (method.MethodKind is not MethodKind.EventAdd
            && method.MethodKind is not MethodKind.EventRemove)
        {
            return null;
        }

        return GetAccessorTargetName(method.Name, "add_")
            ?? GetAccessorTargetName(method.Name, "remove_")
            ?? method.Name;
    }

    private static IMethodSymbol? GetImplementationMethod(ISymbol? implementation, IMethodSymbol interfaceMethod)
    {
        if (implementation is IMethodSymbol method)
        {
            return method;
        }

        if (implementation is IPropertySymbol property)
        {
            return interfaceMethod.MethodKind == MethodKind.PropertySet
                ? property.SetMethod
                : property.GetMethod ?? property.SetMethod;
        }

        if (implementation is IEventSymbol @event)
        {
            return interfaceMethod.MethodKind switch
            {
                MethodKind.EventAdd => @event.AddMethod,
                MethodKind.EventRemove => @event.RemoveMethod,
                _ => @event.AddMethod ?? @event.RemoveMethod
            };
        }

        return null;
    }

    private static string GetMetadataName(INamedTypeSymbol typeSymbol)
    {
        var display = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display.StartsWith("global::", StringComparison.Ordinal)
            ? display.Substring("global::".Length)
            : display;
    }

    private static ISymbol? ResolveInterfaceMember(INamedTypeSymbol interfaceType, IMethodSymbol interfaceMethod)
    {
        var interfaceMember = GetInterfaceMember(interfaceMethod);
        if (interfaceMember is IPropertySymbol property)
        {
            return interfaceType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(member => string.Equals(member.Name, property.Name, StringComparison.Ordinal));
        }

        if (interfaceMember is IEventSymbol @event)
        {
            return interfaceType.GetMembers()
                .OfType<IEventSymbol>()
                .FirstOrDefault(member => string.Equals(member.Name, @event.Name, StringComparison.Ordinal));
        }

        if (interfaceMember is IMethodSymbol method)
        {
            return interfaceType.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, method.Name, StringComparison.Ordinal)
                    && candidate.Parameters.Length == method.Parameters.Length
                    && candidate.Parameters
                        .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        .SequenceEqual(method.Parameters
                            .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                            StringComparer.Ordinal));
        }

        return null;
    }

    private static IMethodSymbol? ResolveImplementationMethod(
        INamedTypeSymbol candidate,
        IMethodSymbol interfaceMethod,
        ISymbol? interfaceMember)
    {
        if (interfaceMember is not null)
        {
            var implementation = candidate.FindImplementationForInterfaceMember(interfaceMember);
            var method = GetImplementationMethod(implementation, interfaceMethod);
            if (method is not null)
            {
                return method;
            }
        }

        var explicitDocIdMatch = FindExplicitImplementationByDocId(candidate, interfaceMember, interfaceMethod);
        if (explicitDocIdMatch is not null)
        {
            return explicitDocIdMatch;
        }

        if (interfaceMethod.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            var propertyName = GetPropertyTargetName(interfaceMethod);
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                var candidateProperty = candidate.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();
                if (candidateProperty is not null)
                {
                    return interfaceMethod.MethodKind == MethodKind.PropertySet
                        ? candidateProperty.SetMethod
                        : candidateProperty.GetMethod ?? candidateProperty.SetMethod;
                }
            }
        }

        if (interfaceMethod.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
        {
            var eventName = GetEventTargetName(interfaceMethod);
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                var candidateEvent = candidate.GetMembers(eventName)
                    .OfType<IEventSymbol>()
                    .FirstOrDefault();
                if (candidateEvent is not null)
                {
                    return interfaceMethod.MethodKind switch
                    {
                        MethodKind.EventAdd => candidateEvent.AddMethod,
                        MethodKind.EventRemove => candidateEvent.RemoveMethod,
                        _ => candidateEvent.AddMethod ?? candidateEvent.RemoveMethod
                    };
                }
            }
        }

        return MatchImplementationBySignature(candidate, interfaceMethod);
    }

    private static IMethodSymbol? FindExplicitImplementationByDocId(
        INamedTypeSymbol candidate,
        ISymbol? interfaceMember,
        IMethodSymbol interfaceMethod)
    {
        if (interfaceMember is IMethodSymbol interfaceMethodSymbol)
        {
            var interfaceDocId = DocumentationIdUtility.GetDocumentationId(interfaceMethodSymbol);
            if (!string.IsNullOrWhiteSpace(interfaceDocId))
            {
                return candidate.GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(method => method.ExplicitInterfaceImplementations.Any(impl =>
                        string.Equals(
                            DocumentationIdUtility.GetDocumentationId(impl),
                            interfaceDocId,
                            StringComparison.Ordinal)));
            }
        }

        if (interfaceMember is IPropertySymbol interfaceProperty)
        {
            var interfaceDocId = DocumentationIdUtility.GetDocumentationId(interfaceProperty);
            if (!string.IsNullOrWhiteSpace(interfaceDocId))
            {
                var match = candidate.GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(property => property.ExplicitInterfaceImplementations.Any(impl =>
                        string.Equals(
                            DocumentationIdUtility.GetDocumentationId(impl),
                            interfaceDocId,
                            StringComparison.Ordinal)));
                if (match is not null)
                {
                    return interfaceMethod.MethodKind == MethodKind.PropertySet
                        ? match.SetMethod
                        : match.GetMethod ?? match.SetMethod;
                }
            }
        }

        if (interfaceMember is IEventSymbol interfaceEvent)
        {
            var interfaceDocId = DocumentationIdUtility.GetDocumentationId(interfaceEvent);
            if (!string.IsNullOrWhiteSpace(interfaceDocId))
            {
                var match = candidate.GetMembers()
                    .OfType<IEventSymbol>()
                    .FirstOrDefault(@event => @event.ExplicitInterfaceImplementations.Any(impl =>
                        string.Equals(
                            DocumentationIdUtility.GetDocumentationId(impl),
                            interfaceDocId,
                            StringComparison.Ordinal)));
                if (match is not null)
                {
                    return interfaceMethod.MethodKind switch
                    {
                        MethodKind.EventAdd => match.AddMethod,
                        MethodKind.EventRemove => match.RemoveMethod,
                        _ => match.AddMethod ?? match.RemoveMethod
                    };
                }
            }
        }

        return null;
    }

    private static IEnumerable<ImplementationCandidate> DeduplicateCandidates(
        IEnumerable<ImplementationCandidate> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var methodIdentity = candidate.MethodDocumentationId
                ?? candidate.Method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var key = $"{candidate.TypeName}|{methodIdentity}";
            if (seen.Add(key))
            {
                yield return candidate;
            }
        }
    }

    private static IMethodSymbol? MatchImplementationBySignature(
        INamedTypeSymbol candidate,
        IMethodSymbol interfaceMethod)
    {
        if (interfaceMethod.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            var propertyName = GetPropertyTargetName(interfaceMethod);
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                var candidateProperty = candidate.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();
                if (candidateProperty is null)
                {
                    return null;
                }

                return interfaceMethod.MethodKind == MethodKind.PropertySet
                    ? candidateProperty.SetMethod
                    : candidateProperty.GetMethod ?? candidateProperty.SetMethod;
            }
        }

        if (interfaceMethod.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
        {
            var eventName = GetEventTargetName(interfaceMethod);
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                var candidateEvent = candidate.GetMembers(eventName)
                    .OfType<IEventSymbol>()
                    .FirstOrDefault();
                if (candidateEvent is null)
                {
                    return null;
                }

                return interfaceMethod.MethodKind switch
                {
                    MethodKind.EventAdd => candidateEvent.AddMethod,
                    MethodKind.EventRemove => candidateEvent.RemoveMethod,
                    _ => candidateEvent.AddMethod ?? candidateEvent.RemoveMethod
                };
            }
        }

        var interfaceMember = GetInterfaceMember(interfaceMethod);
        if (interfaceMember is IPropertySymbol property)
        {
            var candidateProperty = candidate.GetMembers(property.Name)
                .OfType<IPropertySymbol>()
                .FirstOrDefault();
            if (candidateProperty is null)
            {
                return null;
            }

            return interfaceMethod.MethodKind == MethodKind.PropertySet
                ? candidateProperty.SetMethod
                : candidateProperty.GetMethod ?? candidateProperty.SetMethod;
        }

        if (interfaceMember is IEventSymbol @event)
        {
            var candidateEvent = candidate.GetMembers(@event.Name)
                .OfType<IEventSymbol>()
                .FirstOrDefault();
            if (candidateEvent is null)
            {
                return null;
            }

            return interfaceMethod.MethodKind switch
            {
                MethodKind.EventAdd => candidateEvent.AddMethod,
                MethodKind.EventRemove => candidateEvent.RemoveMethod,
                _ => candidateEvent.AddMethod ?? candidateEvent.RemoveMethod
            };
        }

        if (interfaceMember is IMethodSymbol method)
        {
            var parameterSignature = method.Parameters
                .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .ToArray();

            return candidate.GetMembers(method.Name)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(candidateMethod =>
                    candidateMethod.Parameters.Length == parameterSignature.Length
                    && candidateMethod.Parameters
                        .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        .SequenceEqual(parameterSignature, StringComparer.Ordinal));
        }

        return null;
    }

    private static string BuildInterfaceMethodSignature(IMethodSymbol method)
    {
        var namespaceName = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var typeName = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty;
        var methodName = method.MethodKind == MethodKind.Constructor
            ? method.ContainingType?.Name ?? method.Name
            : method.Name;
        var parameters = method.Parameters
            .Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var parameterList = string.Join(",", parameters);

        if (string.IsNullOrWhiteSpace(parameterList))
        {
            parameterList = "none";
        }

        return $"{namespaceName}.{typeName}.{methodName}.{parameterList}";
    }

    private static string GetTypeDisplayName(INamedTypeSymbol symbol)
    {
        var display = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display.StartsWith("global::", StringComparison.Ordinal)
            ? display.Substring("global::".Length)
            : display;
    }

    private static bool IsTestProject(string projectName)
    {
        return projectName.Contains(".Tests", StringComparison.OrdinalIgnoreCase)
            || projectName.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
            || projectName.EndsWith("Test", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ImplementationCandidate(
        string TypeName,
        IMethodSymbol Method,
        string? MethodDocumentationId,
        bool IsTestProject);
}
