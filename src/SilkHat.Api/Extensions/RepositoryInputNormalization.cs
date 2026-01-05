namespace SilkHat.Api.Extensions;

public static class RepositoryInputNormalization
{
    public static bool TryNormalizeRootPath(string rootPath, out string normalizedPath, out string? error)
    {
        try
        {
            normalizedPath = Path.GetFullPath(rootPath.Trim());
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            normalizedPath = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
