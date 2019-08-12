using Newtonsoft.Json;
using WireMock.Server;
using WireMock.Settings;

namespace StubServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = FluentMockServer.Start(new FluentMockServerSettings
            {
                Urls = new[] { "http://localhost:5001/" },
                StartAdminInterface = true,
                ReadStaticMappings = true,
                //UseSSL = true,
                //ProxyAndRecordSettings = new ProxyAndRecordSettings
                //{
                //    Url = "https://jsonplaceholder.typicode.com",
                //    ClientX509Certificate2ThumbprintOrSubjectName = "0E81AA542C1AACBA15A892AD6232591BB2E80D9E",
                //    SaveMapping = false,
                //    SaveMappingToFile = false
                //}
            });
            System.Console.WriteLine("Press any key to stop the server");
            System.Console.ReadKey();
            server.Stop();
            System.Console.WriteLine("Displaying all requests");
            var allRequests = server.LogEntries;
            System.Console.WriteLine(JsonConvert.SerializeObject(allRequests, Formatting.Indented));
            System.Console.WriteLine("Press any key to quit");
            System.Console.ReadKey();
        }
    }
}