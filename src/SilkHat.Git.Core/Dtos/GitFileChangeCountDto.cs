namespace SilkHat.Git.Core.Dtos;

public sealed record GitFileChangeCountDto(
    string Path,
    int ChangeCount);

