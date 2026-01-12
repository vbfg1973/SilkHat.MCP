using Fluxor;

namespace SilkHat.Ui.State
{
    public sealed class FluxorLoggingMiddleware : IMiddleware
    {
        private readonly ILogger<FluxorLoggingMiddleware> _logger;

        public FluxorLoggingMiddleware(ILogger<FluxorLoggingMiddleware> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync(IDispatcher dispatcher, IStore store)
        {
            return Task.CompletedTask;
        }

        public void AfterInitializeAllMiddlewares()
        {
        }

        public bool MayDispatchAction(object action)
        {
            return true;
        }

        public IDisposable BeginInternalMiddlewareChange()
        {
            return NoopDisposable.Instance;
        }

        public void BeforeDispatch(object action)
        {
            _logger.LogDebug("Fluxor dispatch: {ActionType}", action.GetType().Name);
        }

        public void AfterDispatch(object action)
        {
        }

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}