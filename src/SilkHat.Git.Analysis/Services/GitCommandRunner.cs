using System.Diagnostics;
using SilkHat.Git.Analysis.Abstractions;

namespace SilkHat.Git.Analysis.Services
{
    public sealed class GitCommandRunner : IGitCommandRunner
    {
        public async Task<GitCommandResult> ExecuteAsync(string repoRoot, string[] args,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            foreach (var arg in args) startInfo.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            return new GitCommandResult(process.ExitCode, stdOut, stdErr);
        }
    }
}