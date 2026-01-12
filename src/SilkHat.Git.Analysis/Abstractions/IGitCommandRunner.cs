namespace SilkHat.Git.Analysis.Abstractions
{
    public interface IGitCommandRunner
    {
        Task<GitCommandResult> ExecuteAsync(string repoRoot, string[] args, CancellationToken cancellationToken);
    }

    public sealed record GitCommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}