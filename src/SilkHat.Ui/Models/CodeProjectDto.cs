namespace SilkHat.Ui.Models;

public sealed record CodeProjectDto(
    string ProjectKey,
    string Name,
    string Language,
    string AssemblyName);
