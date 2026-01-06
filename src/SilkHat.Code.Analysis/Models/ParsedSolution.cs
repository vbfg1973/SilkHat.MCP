namespace SilkHat.Code.Analysis.Models;

public sealed record ParsedSolution(
    string SolutionPath,
    string SolutionDirectory,
    IReadOnlyList<SolutionProject> Projects);

public sealed record SolutionProject(
    string Name,
    string RelativePath,
    string FullPath,
    string ProjectTypeId,
    string ProjectId);
