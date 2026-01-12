using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services
{
    public sealed class IndexingStatusStore : IIndexingStatusStore
    {
        private readonly Dictionary<string, Dictionary<IndexJobType, IndexJobStatus>> _status
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _sync = new();

        public void InitializeSolution(string solutionId)
        {
            if (string.IsNullOrWhiteSpace(solutionId)) return;

            lock (_sync)
            {
                if (!_status.TryGetValue(solutionId, out var jobs))
                {
                    jobs = new Dictionary<IndexJobType, IndexJobStatus>();
                    _status[solutionId] = jobs;
                }

                foreach (IndexJobType jobType in Enum.GetValues(typeof(IndexJobType)))
                    jobs[jobType] = new IndexJobStatus(jobType, IndexJobState.Pending, 0, null);
            }
        }

        public void SetJobRunning(string solutionId, IndexJobType jobType, string? message = null)
        {
            SetInternal(solutionId, jobType, IndexJobState.Running, 10, message);
        }

        public void SetJobCompleted(string solutionId, IndexJobType jobType, string? message = null)
        {
            SetInternal(solutionId, jobType, IndexJobState.Completed, 100, message);
        }

        public void SetJobFailed(string solutionId, IndexJobType jobType, string? message = null)
        {
            SetInternal(solutionId, jobType, IndexJobState.Failed, 100, message);
        }

        public IReadOnlyCollection<IndexJobStatus> GetStatus(string solutionId)
        {
            if (string.IsNullOrWhiteSpace(solutionId)) return Array.Empty<IndexJobStatus>();

            lock (_sync)
            {
                if (_status.TryGetValue(solutionId, out var jobs)) return jobs.Values.ToList();
            }

            return Array.Empty<IndexJobStatus>();
        }

        private void SetInternal(string solutionId, IndexJobType jobType, IndexJobState state, int percent,
            string? message)
        {
            if (string.IsNullOrWhiteSpace(solutionId)) return;

            lock (_sync)
            {
                if (!_status.TryGetValue(solutionId, out var jobs))
                {
                    jobs = new Dictionary<IndexJobType, IndexJobStatus>();
                    _status[solutionId] = jobs;
                }

                jobs[jobType] = new IndexJobStatus(jobType, state, percent, message);
            }
        }
    }
}