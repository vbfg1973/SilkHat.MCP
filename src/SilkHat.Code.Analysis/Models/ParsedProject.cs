namespace SilkHat.Code.Analysis.Models;

public sealed record ProjectReferenceInfo(
    string IncludePath,
    string FullPath);

public sealed record PackageReferenceInfo(
    string Id,
    string? Version);

public sealed record ParsedProject(
    string ProjectPath,
    string ProjectName,
    string AssemblyName,
    string? RootNamespace,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> DefineConstants,
    string? LangVersion,
    bool EnableDefaultCompileItems,
    IReadOnlyList<ProjectReferenceInfo> ProjectReferences,
    IReadOnlyList<PackageReferenceInfo> PackageReferences,
    IReadOnlyList<string> CompileItems);
