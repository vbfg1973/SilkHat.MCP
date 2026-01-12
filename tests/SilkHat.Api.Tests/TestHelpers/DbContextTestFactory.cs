using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SilkHat.Infrastructure;

namespace SilkHat.Api.Tests.TestHelpers
{
    public sealed class SaveChangesCounterInterceptor : SaveChangesInterceptor
    {
        public int SaveChangesCalls { get; private set; }
        public int SaveChangesAsyncCalls { get; private set; }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            SaveChangesCalls++;
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCalls++;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    public static class DbContextTestFactory
    {
        public static SilkHatDbContext CreateInMemory(SaveChangesCounterInterceptor? interceptor = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SilkHatDbContext>()
                .UseInMemoryDatabase($"silkhat-tests-{Guid.NewGuid():N}");

            if (interceptor is not null) optionsBuilder.AddInterceptors(interceptor);

            var context = new SilkHatDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}