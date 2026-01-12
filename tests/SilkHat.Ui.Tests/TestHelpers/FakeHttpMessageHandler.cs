using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SilkHat.Ui.Tests.TestHelpers
{
    public sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = new(StringComparer.OrdinalIgnoreCase);

        public void AddJsonResponse(string path, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            _responses[path.TrimStart('/')] = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty;
            if (_responses.TryGetValue(path, out var response)) return Task.FromResult(CloneResponse(response));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage CloneResponse(HttpResponseMessage source)
        {
            var clone = new HttpResponseMessage(source.StatusCode);
            if (source.Content is not null)
            {
                var payload = source.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                clone.Content =
                    new StringContent(payload, Encoding.UTF8, source.Content.Headers.ContentType?.MediaType);
            }

            return clone;
        }
    }
}