using Moq;
using SilkHat.Analysis.Abstractions;
using SilkHat.Analysis.Models;
using SilkHat.Analysis.Services;
using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Core.Dtos;
using System.Collections.Generic;

namespace SilkHat.Tests.Services;

public sealed class RepoCommandProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_EmitsEvents_AndCallsDependencies()
    {
        var store = new Mock<ILoadedRepositoryStore>();
        store.Setup(s => s.GetLock(It.IsAny<Guid>())).Returns(new SemaphoreSlim(1, 1));

        var codeStore = new Mock<ICodeWorkspaceStore>();
        var loader = new Mock<ICodeWorkspaceLoader>();
        var context = new RepoCommandContext(Guid.NewGuid(), "/repo", new List<string> { "./Repo.sln" }, store.Object, codeStore.Object, loader.Object);

        var command = new TestCommand();
        var processor = new RepoCommandProcessor();

        var results = new List<RepoEventDto>();
        await foreach (var evt in processor.ExecuteAsync(command, context, CancellationToken.None))
        {
            results.Add(evt);
        }

        Assert.NotEmpty(results);
        Assert.Equal(RepoEventKind.Progress, results[0].Kind);
        Assert.Equal(1, command.ExecuteCallCount);
        store.Verify(s => s.GetLock(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SerializesCommandsPerRepository()
    {
        var store = new LoadedRepositoryStore();
        var codeStore = new Mock<ICodeWorkspaceStore>();
        var loader = new Mock<ICodeWorkspaceLoader>();
        var processor = new RepoCommandProcessor();
        var configId = Guid.NewGuid();

        var context = new RepoCommandContext(configId, "/repo", new List<string> { "./Repo.sln" }, store, codeStore.Object, loader.Object);
        var tracker = new ConcurrencyTracker();
        var command = new BlockingCommand(tracker);

        var task1 = Consume(processor.ExecuteAsync(command, context, CancellationToken.None));
        var task2 = Consume(processor.ExecuteAsync(command, context, CancellationToken.None));

        await Task.WhenAll(task1, task2);

        Assert.Equal(1, tracker.MaxConcurrent);
    }

    private static async Task Consume(IAsyncEnumerable<RepoEventDto> stream)
    {
        await foreach (var _ in stream)
        {
        }
    }

    private sealed class TestCommand : IRepoCommand
    {
        public int ExecuteCallCount { get; private set; }

        public async IAsyncEnumerable<RepoEventDto> ExecuteAsync(
            RepoCommandContext context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecuteCallCount++;
            yield return new RepoEventDto(RepoEventKind.Progress, "stage", "msg", 1, null, null, null);
            await Task.CompletedTask;
        }
    }

    private sealed class BlockingCommand : IRepoCommand
    {
        private readonly ConcurrencyTracker _tracker;

        public BlockingCommand(ConcurrencyTracker tracker)
        {
            _tracker = tracker;
        }

        public async IAsyncEnumerable<RepoEventDto> ExecuteAsync(
            RepoCommandContext context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _tracker.Enter();
            try
            {
                await Task.Delay(25, cancellationToken);
                yield return new RepoEventDto(RepoEventKind.Progress, "stage", "msg", 1, null, null, null);
            }
            finally
            {
                _tracker.Exit();
            }
        }
    }

    private sealed class ConcurrencyTracker
    {
        private int _current;
        private int _max;

        public int MaxConcurrent => _max;

        public void Enter()
        {
            var current = Interlocked.Increment(ref _current);
            var previous = _max;
            while (current > previous)
            {
                var updated = Interlocked.CompareExchange(ref _max, current, previous);
                if (updated == previous)
                {
                    break;
                }

                previous = updated;
            }
        }

        public void Exit()
        {
            Interlocked.Decrement(ref _current);
        }
    }
}
