using SilkHat.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Analysis.Abstractions;

public interface IRepoCommand
{
    IAsyncEnumerable<RepoEventDto> ExecuteAsync(RepoCommandContext context, CancellationToken cancellationToken);
}
