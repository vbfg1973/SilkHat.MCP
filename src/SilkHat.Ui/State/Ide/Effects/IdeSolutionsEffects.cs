using Fluxor;
using Microsoft.Extensions.Logging;
using SilkHat.Ui.Services;
using SilkHat.Ui.State.Ide.Actions;
using SilkHat.Ui.State.Ide;

namespace SilkHat.Ui.State.Ide.Effects;

public sealed class IdeSolutionsEffects
{
    private readonly RepositoryApiClient _api;
    private readonly ILogger<IdeSolutionsEffects> _logger;

    public IdeSolutionsEffects(RepositoryApiClient api, ILogger<IdeSolutionsEffects> logger)
    {
        _api = api;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadSolutions(LoadSolutionsAction action, IDispatcher dispatcher)
    {
        _logger.LogDebug("IDE: loading solutions");
        try
        {
            var configs = await _api.GetLoadedRepositoryConfigsAsync();
            var solutions = new List<IdeSolutionEntry>();
            foreach (var config in configs.Items)
            {
                foreach (var solution in config.Solutions.Where(item => item.IsEnabled))
                {
                    solutions.Add(new IdeSolutionEntry(
                        solution.SolutionId,
                        solution.RelativePath,
                        solution.RelativePath,
                        config.Name,
                        config.Id,
                        $"{solution.RelativePath} ({config.Name})"));
                }
            }

            solutions.Sort((left, right) =>
            {
                var nameComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
                if (nameComparison != 0)
                {
                    return nameComparison;
                }

                return string.Compare(left.SolutionId, right.SolutionId, StringComparison.OrdinalIgnoreCase);
            });

            dispatcher.Dispatch(new LoadSolutionsSuccessAction(solutions));
            if (solutions.Count > 0)
            {
                dispatcher.Dispatch(new SelectSolutionAction(solutions[0].SolutionId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IDE: failed to load solutions");
            dispatcher.Dispatch(new LoadSolutionsFailureAction(ex.Message));
        }
    }
}
