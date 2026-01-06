namespace SilkHat.Sample.Lib;

public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

public static class LibConstants
{
    public const string DefaultLabel = "Sample";
}
