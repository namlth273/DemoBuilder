using Microsoft.Extensions.Configuration;
using System.IO;

namespace DemoBuilder.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IEnvironmentService _environmentService;

        public ConfigurationService(IEnvironmentService environmentService)
        {
            _environmentService = environmentService;
        }

        /// <summary>
        /// Get Configuration object which contains environment variables value being built from appsettings.json file or environment variables.
        /// </summary>
        /// <returns></returns>
        public IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{_environmentService.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}