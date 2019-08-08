using Autofac;
using Autofac.Extensions.DependencyInjection;
using DemoBuilder.Services;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DemoBuilder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var container = ConfigureContainer();

            FlurlHttp.Configure(c => c.OnErrorAsync = call =>
            {
                Console.WriteLine("On exception...");
                Console.WriteLine(call.Exception.Message);
                //call.ExceptionHandled = true;
                return Task.CompletedTask;
            });

            FlurlHttp.Configure(c => c.BeforeCallAsync = call =>
            {
                Console.WriteLine("Before call...");
                Console.WriteLine(JsonConvert.SerializeObject(call.FlurlRequest.Url));
                return Task.CompletedTask;
            });

            using (var scope = container.BeginLifetimeScope())
            {
                //var clientFactory = scope.Resolve<IHttpClientFactory>();

                //var client = clientFactory.CreateClient("DemoClient");

                //var request = new HttpRequestMessage(HttpMethod.Post, "/api/product/getall");
                //var getRequest = new HttpRequestMessage(HttpMethod.Get, "/comments?postId=1");

                //var response = await client.SendAsync(request);

                //await response.EnsureSuccessStatusCodeAsync();

                //var products = await response.Content.ReadAsAsync<IList<Product>>();

                //var demoClient = scope.Resolve<IDemoClient>();

                //var products = await demoClient.SendAsync<List<Product>>(request);

                //var value = await demoClient.SendAsync<dynamic>(getRequest);

                //Console.WriteLine(JsonConvert.SerializeObject(products, Formatting.Indented));

                //Console.WriteLine(value);

                //var getResp = await "https://jsonplaceholder.typicode.com/comments?postId=1".GetJsonListAsync();

                //Console.WriteLine(JsonConvert.SerializeObject(getResp, Formatting.Indented));

                var client = scope.Resolve<IDemoFlurlClient>();

                var result = await client.GetAsync<IList<Comment>>("comments1?postId=1");

                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

            }
        }

        static IContainer ConfigureContainer()
        {
            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());
            services.AddHttpClient<IDemoClient, DemoClient>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IConfigurationService>().GetConfiguration();
                var baseUri = new Uri(config["BASE_URL"]);

                client.BaseAddress = new Uri(baseUri.ToString());
                //client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            services.Replace(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, CustomLoggingFilter>());

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterModule<AutofacModule>();
            return builder.Build();
        }
    }
}
