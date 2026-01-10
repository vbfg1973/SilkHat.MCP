using Fluxor;
using Microsoft.Extensions.Logging;
using SilkHat.Ui.Models;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide.Models;

namespace SilkHat.Ui.State.Ide.Effects;

public sealed class IdeTabsEffects
{
    private readonly RepositoryApiClient _api;
    private readonly IState<IdeSolutionsState> _solutionsState;
    private readonly IState<IdeTabsState> _tabsState;
    private readonly ILogger<IdeTabsEffects> _logger;

    public IdeTabsEffects(
        RepositoryApiClient api,
        IState<IdeSolutionsState> solutionsState,
        IState<IdeTabsState> tabsState,
        ILogger<IdeTabsEffects> logger)
    {
        _api = api;
        _solutionsState = solutionsState;
        _tabsState = tabsState;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleOpenFile(OpenFileTabAction action, IDispatcher dispatcher)
    {
        if (action.Entry.Type != CodeTreeEntryType.File)
        {
            return;
        }

        _logger.LogDebug("IDE: opening file {Path}", action.Entry.DisplayPath);
        try
        {
            var content = await _api.GetCodeFileAsync(action.ConfigId, action.SolutionId, action.Entry.DisplayPath);
            var tab = new IdeOpenFileTab(
                action.Entry.RepositoryPath,
                action.Entry.DisplayPath,
                action.Entry.Name,
                content.Content);

            try
            {
                var lastChange = await _api.GetGitFileLastChangeAsync(action.ConfigId, action.Entry.RepositoryPath, false);
                tab.LastCommitAuthor = lastChange.Author;
                tab.LastCommitAuthorEmail = lastChange.AuthorEmail;
                tab.LastCommitDateUtc = lastChange.CommitDateUtc;
                tab.AbbreviatedSha = string.IsNullOrWhiteSpace(lastChange.AbbreviatedSha)
                    ? lastChange.CommitSha
                    : lastChange.AbbreviatedSha;
                tab.LastCommitSubject = lastChange.Subject;

                var changeCount = await _api.GetGitFileChangeCountAsync(action.ConfigId, action.Entry.RepositoryPath);
                tab.ChangeCount = changeCount.ChangeCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IDE: unable to load last commit metadata for {Path}", action.Entry.DisplayPath);
            }

            dispatcher.Dispatch(new OpenFileTabSuccessAction(action.SolutionId, tab));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to open file {Path}", action.Entry.DisplayPath);
            dispatcher.Dispatch(new OpenFileTabFailureAction(action.SolutionId, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleOpenFileByPath(OpenFileTabByPathAction action, IDispatcher dispatcher)
    {
        var view = _tabsState.Value.Tabs.TryGetValue(action.SolutionId, out var existing)
            ? existing
            : null;
        if (view is not null)
        {
            var existingIndex = view.OpenFiles.FindIndex(file =>
                string.Equals(file.RepositoryPath, action.RepositoryPath, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                dispatcher.Dispatch(new FocusFileTabAction(
                    action.SolutionId,
                    existingIndex,
                    action.HighlightLine,
                    action.HighlightStartLine,
                    action.HighlightEndLine));
                return;
            }
        }

        _logger.LogDebug("IDE: opening file by path {Path}", action.RepositoryPath);
        try
        {
            var content = await _api.GetCodeFileAsync(action.ConfigId, action.SolutionId, action.RepositoryPath);
            var displayPath = string.IsNullOrWhiteSpace(content.DisplayPath) ? action.RepositoryPath : content.DisplayPath;
            var name = Path.GetFileName(displayPath);
            var tab = new IdeOpenFileTab(
                content.RepositoryPath,
                displayPath,
                name,
                content.Content)
            {
                HighlightLine = action.HighlightLine,
                HighlightStartLine = action.HighlightStartLine,
                HighlightEndLine = action.HighlightEndLine
            };

            tab.RenderLines = IdeTabHelpers.BuildAnnotatedLines(
                tab.Content,
                tab.DiffLines,
                tab.ShowDiff,
                tab.HighlightLine,
                tab.HighlightStartLine,
                tab.HighlightEndLine);

            try
            {
                var lastChange = await _api.GetGitFileLastChangeAsync(action.ConfigId, tab.RepositoryPath, false);
                tab.LastCommitAuthor = lastChange.Author;
                tab.LastCommitAuthorEmail = lastChange.AuthorEmail;
                tab.LastCommitDateUtc = lastChange.CommitDateUtc;
                tab.AbbreviatedSha = string.IsNullOrWhiteSpace(lastChange.AbbreviatedSha)
                    ? lastChange.CommitSha
                    : lastChange.AbbreviatedSha;
                tab.LastCommitSubject = lastChange.Subject;

                var changeCount = await _api.GetGitFileChangeCountAsync(action.ConfigId, tab.RepositoryPath);
                tab.ChangeCount = changeCount.ChangeCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IDE: unable to load last commit metadata for {Path}", tab.DisplayPath);
            }

            dispatcher.Dispatch(new OpenFileTabSuccessAction(action.SolutionId, tab));
            if (action.HighlightLine.HasValue)
            {
                var updatedView = _tabsState.Value.Tabs.TryGetValue(action.SolutionId, out var after)
                    ? after
                    : null;
                if (updatedView is not null)
                {
                    var newIndex = updatedView.OpenFiles.FindIndex(file =>
                        string.Equals(file.RepositoryPath, tab.RepositoryPath, StringComparison.OrdinalIgnoreCase));
                    if (newIndex >= 0)
                    {
                        dispatcher.Dispatch(new FocusFileTabAction(
                            action.SolutionId,
                            newIndex,
                            action.HighlightLine,
                            action.HighlightStartLine,
                            action.HighlightEndLine));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to open file {Path}", action.RepositoryPath);
            dispatcher.Dispatch(new OpenFileTabFailureAction(action.SolutionId, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleToggleDiff(ToggleDiffAction action, IDispatcher dispatcher)
    {
        if (!action.Enabled)
        {
            return;
        }

        var view = _tabsState.Value.Tabs.TryGetValue(action.SolutionId, out var state)
            ? state
            : null;
        if (view is null || view.OpenFiles.Count == 0)
        {
            return;
        }

        var index = view.ActiveTabIndex;
        if (index < 0 || index >= view.OpenFiles.Count)
        {
            return;
        }

        var file = view.OpenFiles[index];
        if (file.DiffLoaded)
        {
            return;
        }

        var solutionEntry = _solutionsState.Value.Solutions
            .FirstOrDefault(solution => string.Equals(solution.SolutionId, action.SolutionId, StringComparison.OrdinalIgnoreCase));
        if (solutionEntry is null)
        {
            return;
        }

        _logger.LogDebug("IDE: loading diff for {Path}", file.DisplayPath);
        try
        {
            var lastChange = await _api.GetGitFileLastChangeAsync(solutionEntry.ConfigId, file.RepositoryPath, true);
            file.LastCommitAuthor = lastChange.Author;
            file.LastCommitAuthorEmail = lastChange.AuthorEmail;
            file.LastCommitDateUtc = lastChange.CommitDateUtc;
            file.AbbreviatedSha = string.IsNullOrWhiteSpace(lastChange.AbbreviatedSha)
                ? lastChange.CommitSha
                : lastChange.AbbreviatedSha;
            file.LastCommitSubject = lastChange.Subject;
            file.DiffLines = lastChange.DiffLines.ToList();
            file.DiffLoaded = true;
            file.RenderLines = IdeTabHelpers.BuildAnnotatedLines(
                file.Content,
                file.DiffLines,
                file.ShowDiff,
                file.HighlightLine,
                file.HighlightStartLine,
                file.HighlightEndLine);

            if (file.ChangeCount is null)
            {
                var changeCount = await _api.GetGitFileChangeCountAsync(solutionEntry.ConfigId, file.RepositoryPath);
                file.ChangeCount = changeCount.ChangeCount;
            }

            dispatcher.Dispatch(new ToggleDiffSuccessAction(action.SolutionId, file));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IDE: failed to load diff for {Path}", file.DisplayPath);
        }
    }
}
