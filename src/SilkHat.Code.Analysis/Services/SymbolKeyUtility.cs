using System.Reflection;
using Microsoft.CodeAnalysis;

namespace SilkHat.Code.Analysis.Services;

public static class SymbolKeyUtility
{
    private static readonly MethodInfo? GetSymbolKeyStringMethod = ResolveGetSymbolKeyStringMethod();

    public static string GetSymbolKeyString(ISymbol symbol, Compilation compilation)
    {
        if (GetSymbolKeyStringMethod is null)
        {
            throw new InvalidOperationException("SymbolKey support is not available in the current Roslyn assemblies.");
        }

        return (string)GetSymbolKeyStringMethod.Invoke(null, new object?[] { symbol, compilation })!;
    }

    private static MethodInfo? ResolveGetSymbolKeyStringMethod()
    {
        var workspaceAssembly = typeof(Workspace).Assembly;
        var extensionsType = workspaceAssembly.GetType("Microsoft.CodeAnalysis.SymbolKeyExtensions");
        if (extensionsType is null)
        {
            return null;
        }

        return extensionsType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                if (!string.Equals(method.Name, "GetSymbolKeyString", StringComparison.Ordinal))
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 2
                       && parameters[0].ParameterType == typeof(ISymbol)
                       && parameters[1].ParameterType == typeof(Compilation);
            });
    }
}
