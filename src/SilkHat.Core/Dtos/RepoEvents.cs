namespace SilkHat.Core.Dtos;

public static class RepoEventKind
{
    public const string Progress = "progress";
    public const string Data = "data";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public sealed record RepoEventErrorDto(
    string Message,
    string? Detail);

public sealed record RepoEventSummaryDto(
    string Message);

public sealed record RepoEventDto(
    string Kind,
    string? Stage,
    string? Message,
    double? Percent,
    object? Payload,
    RepoEventErrorDto? Error,
    RepoEventSummaryDto? Summary);
