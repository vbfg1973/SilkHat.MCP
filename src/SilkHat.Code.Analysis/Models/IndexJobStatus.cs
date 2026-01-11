namespace SilkHat.Code.Analysis.Models;

public enum IndexJobType
{
    ProjectsAndFiles = 0,
    TypesAndMembers = 1,
    Packages = 2,
    Git = 3,
    Complexity = 4
}

public enum IndexJobState
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}

public sealed record IndexJobStatus(
    IndexJobType JobType,
    IndexJobState State,
    int Percent,
    string? Message);
