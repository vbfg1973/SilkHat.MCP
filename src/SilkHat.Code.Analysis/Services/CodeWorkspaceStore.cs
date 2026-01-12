using System.Collections.Concurrent;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services
{
    public sealed class CodeWorkspaceStore : ICodeWorkspaceStore
    {
        private readonly ConcurrentDictionary<Guid, CodeRepositoryWorkspace> _workspaces = new();

        public CodeRepositoryWorkspace? Get(Guid configId)
        {
            return _workspaces.TryGetValue(configId, out var workspace) ? workspace : null;
        }

        public void Set(Guid configId, CodeRepositoryWorkspace workspace)
        {
            _workspaces[configId] = workspace;
        }

        public bool Remove(Guid configId)
        {
            return _workspaces.TryRemove(configId, out _);
        }
    }
}