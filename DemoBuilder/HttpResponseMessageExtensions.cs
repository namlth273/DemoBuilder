using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoBuilder
{
    public static class HttpResponseMessageExtensions
    {
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

            throw new SimpleHttpResponseException(response.StatusCode, content);
        }
    }

    public class SimpleHttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content)
        {
            StatusCode = statusCode;
        }
    }
}