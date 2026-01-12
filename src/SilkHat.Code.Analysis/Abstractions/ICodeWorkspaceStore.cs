using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions
{
    public interface ICodeWorkspaceStore
    {
        CodeRepositoryWorkspace? Get(Guid configId);
        void Set(Guid configId, CodeRepositoryWorkspace workspace);
        bool Remove(Guid configId);
    }
}