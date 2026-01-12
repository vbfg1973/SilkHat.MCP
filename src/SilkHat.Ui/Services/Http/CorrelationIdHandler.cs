namespace SilkHat.Ui.Services.Http
{
    public sealed class CorrelationIdHandler : DelegatingHandler
    {
        private const string CorrelationHeader = "X-Correlation-Id";
        private readonly ILogger<CorrelationIdHandler> _logger;

        public CorrelationIdHandler(ILogger<CorrelationIdHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var localCorrelationId = Guid.NewGuid().ToString("N");
            if (!request.Headers.Contains(CorrelationHeader))
                request.Headers.Add(CorrelationHeader, localCorrelationId);

            _logger.LogDebug("HTTP {Method} {Url} correlation {CorrelationId}",
                request.Method.Method,
                request.RequestUri,
                localCorrelationId);

            var response = await base.SendAsync(request, cancellationToken);
            response.Headers.TryGetValues(CorrelationHeader, out var responseValues);
            var responseCorrelationId = responseValues?.FirstOrDefault();

            if (response.IsSuccessStatusCode)
                _logger.LogDebug(
                    "HTTP {Method} {Url} -> {StatusCode} correlation {CorrelationId} response {ResponseCorrelationId}",
                    request.Method.Method,
                    request.RequestUri,
                    (int)response.StatusCode,
                    localCorrelationId,
                    responseCorrelationId);
            else
                _logger.LogWarning(
                    "HTTP {Method} {Url} -> {StatusCode} correlation {CorrelationId} response {ResponseCorrelationId}",
                    request.Method.Method,
                    request.RequestUri,
                    (int)response.StatusCode,
                    localCorrelationId,
                    responseCorrelationId);

            return response;
        }
    }
}