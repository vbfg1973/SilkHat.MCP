using System.Collections.Concurrent;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Core.Dtos;
using SilkHat.Git.Analysis.Abstractions;

namespace SilkHat.Api.Services
{
    public interface ICodeTreeMetricsPrecomputeService
    {
        Task StartPrecomputeAsync(Guid configId, CancellationToken cancellationToken);
        void Clear(Guid configId);
    }

    public sealed class CodeTreeMetricsPrecomputeService : ICodeTreeMetricsPrecomputeService
    {
        private readonly ICodeTreeMetricsCacheStore _cacheStore;
        private readonly ICodeWorkspaceStore _codeWorkspaceStore;
        private readonly IComplexityMetricsAggregator _complexityAggregator;
        private readonly IGitMetricsAggregator _gitMetricsAggregator;
        private readonly ILogger<CodeTreeMetricsPrecomputeService> _logger;
        private readonly ICodeTreeMetricsService _metricsService;
        private readonly ConcurrentDictionary<Guid, Task> _running = new();

        public CodeTreeMetricsPrecomputeService(
            ICodeWorkspaceStore codeWorkspaceStore,
            ICodeTreeMetricsService metricsService,
            ICodeTreeMetricsCacheStore cacheStore,
            IComplexityMetricsAggregator complexityAggregator,
            IGitMetricsAggregator gitMetricsAggregator,
            ILogger<CodeTreeMetricsPrecomputeService> logger)
        {
            _codeWorkspaceStore = codeWorkspaceStore;
            _metricsService = metricsService;
            _cacheStore = cacheStore;
            _complexityAggregator = complexityAggregator;
            _gitMetricsAggregator = gitMetricsAggregator;
            _logger = logger;
        }

        public Task StartPrecomputeAsync(Guid configId, CancellationToken cancellationToken)
        {
            var task = _running.GetOrAdd(configId, _ => Task.Run(() => RunAsync(configId), CancellationToken.None));
            return task.IsCompleted ? Task.CompletedTask : task;
        }

        public void Clear(Guid configId)
        {
            _cacheStore.Clear(configId);
            _complexityAggregator.Invalidate(configId);
            _gitMetricsAggregator.Invalidate(configId);
            _running.TryRemove(configId, out _);
        }

        private async Task RunAsync(Guid configId)
        {
            try
            {
                var workspace = _codeWorkspaceStore.Get(configId);
                if (workspace is null) return;

                var solutions = workspace.Solutions.Values.ToList();
                var kinds = Enum.GetValues<CodeTreeAnnotationKind>();

                var tasks = new List<Task>(solutions.Count * kinds.Length);
                foreach (var solution in solutions)
                foreach (var kind in kinds)
                    tasks.Add(_metricsService.GetMetricsAsync(configId, workspace, solution, kind,
                        CancellationToken.None));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to precompute code tree metrics for {ConfigId}.", configId);
            }
            finally
            {
                _running.TryRemove(configId, out _);
            }
        }
    }
}