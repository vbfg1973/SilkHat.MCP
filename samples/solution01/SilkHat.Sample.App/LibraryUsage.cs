using SilkHat.Sample.Lib;

namespace SilkHat.Sample.App;

public sealed class LibraryUsage
{
    private readonly IClock _clock;

    public LibraryUsage(IClock clock)
    {
        _clock = clock;
    }

    public string Describe()
    {
        return $"{LibConstants.DefaultLabel} at {_clock.Now:O}";
    }
}
