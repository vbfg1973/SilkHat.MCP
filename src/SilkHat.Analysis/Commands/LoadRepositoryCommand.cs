using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Analysis.Commands;

public sealed class LoadRepositoryCommand : IRepoCommand
{
    public async IAsyncEnumerable<RepoEventDto> ExecuteAsync(
        RepoCommandContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (context.Store.Get(context.ConfigId) is not null)
        {
            yield return new RepoEventDto(
                RepoEventKind.Completed,
                "load",
                "Repository already loaded.",
                100,
                null,
                null,
                new RepoEventSummaryDto("Already loaded."));
            yield break;
        }

        yield return new RepoEventDto(
            RepoEventKind.Progress,
            "validate",
            "Validating repository configuration.",
            10,
            null,
            null,
            null);

        await Task.Delay(150, cancellationToken);

        yield return new RepoEventDto(
            RepoEventKind.Progress,
            "workspace",
            "Preparing repository workspace placeholder.",
            60,
            null,
            null,
            null);

        await Task.Delay(200, cancellationToken);

        yield return new RepoEventDto(
            RepoEventKind.Progress,
            "code-analysis",
            "Loading solutions and building code index.",
            75,
            null,
            null,
            null);

        var workspace = await context.CodeWorkspaceLoader.LoadAsync(context.RootPath, context.SolutionPaths, cancellationToken);
        context.CodeWorkspaceStore.Set(context.ConfigId, workspace);

        context.Store.SetLoaded(context.ConfigId, context.RootPath);

        yield return new RepoEventDto(
            RepoEventKind.Completed,
            "load",
            "Repository loaded.",
            100,
            null,
            null,
            new RepoEventSummaryDto("Loaded placeholder workspace."));
    }
}
