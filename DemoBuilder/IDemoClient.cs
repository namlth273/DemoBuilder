using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DemoBuilder
{
    public interface IDemoClient
    {
        Task<T> SendAsync<T>(HttpRequestMessage request);
        Task<T> GetAsync<T>(string route);
    }

    public class DemoClient : IDemoClient
    {
        private readonly HttpClient _client;

        public DemoClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            var response = await _client.SendAsync(request);

            await response.EnsureSuccessStatusCodeAsync();

            var items = await response.Content.ReadAsAsync<T>();

            return items;
        }

        public async Task<T> GetAsync<T>(string route)
        {
            var response = await _client.GetAsync(route);

            await response.EnsureSuccessStatusCodeAsync();

            var items = await response.Content.ReadAsAsync<T>();

            return items;
        }
    }

    public class CustomLoggingFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly ILoggerFactory _loggerFactory;

        public CustomLoggingFilter(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            return (builder) =>
            {
                // Run other configuration first, we want to decorate.
                next(builder);

                var outerLogger =
                    _loggerFactory.CreateLogger($"System.Net.Http.HttpClient.{builder.Name}.LogicalHandler");

                builder.AdditionalHandlers.Insert(0, new CustomLoggingScopeHttpMessageHandler(outerLogger));
            };
        }
    }

    public class CustomLoggingScopeHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public CustomLoggingScopeHttpMessageHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (Log.BeginRequestPipelineScope(_logger, request))
            {
                Log.RequestPipelineStart(_logger, request);
                var response = await base.SendAsync(request, cancellationToken);
                Log.RequestPipelineEnd(_logger, response);

                return response;
            }
        }

        private static class Log
        {
            private static class EventIds
            {
                public static readonly EventId PipelineStart = new EventId(100, "RequestPipelineStart");
                public static readonly EventId PipelineEnd = new EventId(101, "RequestPipelineEnd");
            }

            private static readonly Func<ILogger, HttpMethod, Uri, string, IDisposable> _beginRequestPipelineScope =
                LoggerMessage.DefineScope<HttpMethod, Uri, string>(
                    "HTTP {HttpMethod} {Uri} {CorrelationId}");

            private static readonly Action<ILogger, HttpMethod, Uri, string, Exception> _requestPipelineStart =
                LoggerMessage.Define<HttpMethod, Uri, string>(
                    LogLevel.Information,
                    EventIds.PipelineStart,
                    "Start processing HTTP request {HttpMethod} {Uri} [Correlation: {CorrelationId}]");

            private static readonly Action<ILogger, HttpStatusCode, Exception> _requestPipelineEnd =
                LoggerMessage.Define<HttpStatusCode>(
                    LogLevel.Information,
                    EventIds.PipelineEnd,
                    "End processing HTTP request - {StatusCode}");

            public static IDisposable BeginRequestPipelineScope(ILogger logger, HttpRequestMessage request)
            {
                var correlationId = GetCorrelationIdFromRequest(request);
                return _beginRequestPipelineScope(logger, request.Method, request.RequestUri, correlationId);
            }

            public static void RequestPipelineStart(ILogger logger, HttpRequestMessage request)
            {
                var correlationId = GetCorrelationIdFromRequest(request);
                _requestPipelineStart(logger, request.Method, request.RequestUri, correlationId, null);
            }

            public static void RequestPipelineEnd(ILogger logger, HttpResponseMessage response)
            {
                _requestPipelineEnd(logger, response.StatusCode, null);
            }

            private static string GetCorrelationIdFromRequest(HttpRequestMessage request)
            {
                var correlationId = "Not set";

                if (request.Headers.TryGetValues("X-Correlation-ID", out var values))
                {
                    correlationId = values.First();
                }

                return correlationId;
            }
        }
    }
}