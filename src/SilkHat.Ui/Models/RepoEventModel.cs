namespace SilkHat.Ui.Models;

public sealed record RepoEventErrorModel(
    string Message,
    string? Detail);

public sealed record RepoEventSummaryModel(
    string Message);

public sealed record RepoEventModel(
    string Kind,
    string? Stage,
    string? Message,
    double? Percent,
    object? Payload,
    RepoEventErrorModel? Error,
    RepoEventSummaryModel? Summary);

public sealed record IndexJobStatusModel(
    string JobType,
    string State,
    int Percent,
    string? Message);
