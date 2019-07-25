using System.Net.Http;
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
}