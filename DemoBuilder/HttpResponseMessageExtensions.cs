using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoBuilder
{
    public static class HttpResponseMessageExtensions
    {
        private static readonly IDictionary<HttpStatusCode, Func<string, Exception>> ExceptionStrategies =
            new Dictionary<HttpStatusCode, Func<string, Exception>>
            {
                {HttpStatusCode.Unauthorized, s => new UnAuthorizedException(s)},
                {HttpStatusCode.NotFound, s => new NotFoundException(s)},
            };

        public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            response.Content?.Dispose();

            if (string.IsNullOrEmpty(content))
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception exception)
                {
                    content = exception.Message;
                }
            }

            if (ExceptionStrategies.ContainsKey(response.StatusCode))
                throw ExceptionStrategies[response.StatusCode].Invoke(content);

            throw new SimpleHttpResponseException(response.StatusCode,
                $"StatusCode = {response.StatusCode} {(int)response.StatusCode}. Error = {content}");
        }
    }

    public class SimpleHttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; protected set; }

        public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content)
        {
            StatusCode = statusCode;
        }
    }

    public class UnAuthorizedException : SimpleHttpResponseException
    {
        public UnAuthorizedException(string content) : base(HttpStatusCode.Unauthorized, content)
        {
            StatusCode = HttpStatusCode.Unauthorized;
        }
    }

    public class NotFoundException : SimpleHttpResponseException
    {
        public NotFoundException(string content) : base(HttpStatusCode.NotFound, content)
        {
            StatusCode = HttpStatusCode.NotFound;
        }
    }
}