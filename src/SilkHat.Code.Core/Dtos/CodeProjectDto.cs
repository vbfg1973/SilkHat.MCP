namespace SilkHat.Code.Core.Dtos;

public sealed record CodeProjectDto(
    string ProjectKey,
    string Name,
    string Language,
    string AssemblyName);
