using SilkHat.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Analysis.Abstractions
{
    public interface IRepoCommandProcessor
    {
        IAsyncEnumerable<RepoEventDto> ExecuteAsync(IRepoCommand command, RepoCommandContext context,
            CancellationToken cancellationToken);
    }
}