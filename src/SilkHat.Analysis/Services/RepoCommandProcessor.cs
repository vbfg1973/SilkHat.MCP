using System.Threading.Channels;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Analysis.Services;

public sealed class RepoCommandProcessor : IRepoCommandProcessor
{
    public IAsyncEnumerable<RepoEventDto> ExecuteAsync(IRepoCommand command, RepoCommandContext context, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<RepoEventDto>();

        _ = Task.Run(async () =>
        {
            await using var _ = cancellationToken.Register(() => channel.Writer.TryComplete());
            try
            {
                var semaphore = context.Store.GetLock(context.ConfigId);
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await foreach (var evt in command.ExecuteAsync(context, cancellationToken))
                    {
                        await channel.Writer.WriteAsync(evt, cancellationToken);
                    }
                }
                finally
                {
                    semaphore.Release();
                }

                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryWrite(new RepoEventDto(
                    RepoEventKind.Failed,
                    null,
                    ex.Message,
                    null,
                    null,
                    new RepoEventErrorDto(ex.Message, ex.ToString()),
                    null));
                channel.Writer.TryComplete(ex);
            }
        }, cancellationToken);

        return channel.Reader.ReadAllAsync(cancellationToken);
    }
}
