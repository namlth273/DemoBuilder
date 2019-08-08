using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace DemoBuilder
{
    public interface IDemoFlurlClient
    {
        Task<T> GetAsync<T>(string route);
    }

    public class DemoFlurlClient : IDemoFlurlClient
    {
        private readonly IFlurlClient _client;

        public DemoFlurlClient(IFlurlClientFactory factory)
        {
            _client = factory.Get("https://jsonplaceholder.typicode.com/");
        }

        public Task<T> GetAsync<T>(string route)
        {
            return _client.Request(route).GetJsonAsync<T>();
        }
    }

    public class Comment
    {
        public int PostId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Body { get; set; }
    }
}