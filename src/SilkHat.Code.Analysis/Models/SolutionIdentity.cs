namespace SilkHat.Code.Analysis.Models;

public static class SolutionIdentity
{
    public static string Create(string rootPath, string solutionPath, Guid? solutionGuid)
    {
        if (solutionGuid.HasValue)
        {
            return solutionGuid.Value.ToString("N");
        }

        var relative = NormalizeRelativePath(rootPath, solutionPath);
        return CreateDeterministicGuid(relative).ToString("N");
    }

    public static string NormalizeRelativePath(string rootPath, string fullPath)
    {
        var relative = Path.GetRelativePath(rootPath, fullPath);
        relative = relative.Replace('\\', '/');
        if (!relative.StartsWith(".", StringComparison.Ordinal))
        {
            relative = "./" + relative;
        }

        return relative;
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
