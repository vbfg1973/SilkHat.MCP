using System.Reflection;
using Microsoft.CodeAnalysis;

namespace SilkHat.Code.Analysis.Services
{
    public static class SymbolKeyUtility
    {
        private static readonly MethodInfo? GetSymbolKeyStringMethod = ResolveGetSymbolKeyStringMethod();
        private static readonly MethodInfo? ResolveSymbolKeyMethod = ResolveResolveStringMethod();

        public static string GetSymbolKeyString(ISymbol symbol, Compilation compilation)
        {
            if (GetSymbolKeyStringMethod is null)
                return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            return (string)GetSymbolKeyStringMethod.Invoke(null, new object?[] { symbol, compilation })!;
        }

        public static ISymbol? ResolveSymbol(string symbolKey, Compilation compilation)
        {
            if (ResolveSymbolKeyMethod is null) return null;

            var parameters = ResolveSymbolKeyMethod.GetParameters();
            var resolution = parameters.Length switch
            {
                2 => ResolveSymbolKeyMethod.Invoke(null, new object?[] { symbolKey, compilation }),
                3 => ResolveSymbolKeyMethod.Invoke(null,
                    new object?[] { symbolKey, compilation, CancellationToken.None }),
                _ => null
            };

            if (resolution is null) return null;

            var symbolProperty = resolution.GetType().GetProperty("Symbol");
            return symbolProperty?.GetValue(resolution) as ISymbol;
        }

        private static MethodInfo? ResolveGetSymbolKeyStringMethod()
        {
            var workspaceAssembly = typeof(Workspace).Assembly;
            var extensionsType = workspaceAssembly.GetType("Microsoft.CodeAnalysis.SymbolKeyExtensions");
            if (extensionsType is null) return null;

            return extensionsType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, "GetSymbolKeyString", StringComparison.Ordinal)) return false;

                    var parameters = method.GetParameters();
                    return parameters.Length == 2
                           && parameters[0].ParameterType == typeof(ISymbol)
                           && parameters[1].ParameterType == typeof(Compilation);
                });
        }

        private static MethodInfo? ResolveResolveStringMethod()
        {
            var workspaceAssembly = typeof(Workspace).Assembly;
            var symbolKeyType = workspaceAssembly.GetType("Microsoft.CodeAnalysis.SymbolKey");
            if (symbolKeyType is null) return null;

            return symbolKeyType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, "ResolveString", StringComparison.Ordinal)) return false;

                    var parameters = method.GetParameters();
                    if (parameters.Length < 2 || parameters.Length > 3) return false;

                    if (parameters[0].ParameterType != typeof(string) ||
                        parameters[1].ParameterType != typeof(Compilation)) return false;

                    if (parameters.Length == 3 && parameters[2].ParameterType != typeof(CancellationToken))
                        return false;

                    return true;
                });
        }
    }
}