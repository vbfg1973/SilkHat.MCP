namespace SilkHat.Analysis.Models;

public sealed record LoadedRepository(
    Guid ConfigId,
    string RootPath,
    DateTimeOffset LoadedUtc);
