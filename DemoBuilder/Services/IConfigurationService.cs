using Microsoft.Extensions.Configuration;

namespace DemoBuilder.Services
{
    public interface IConfigurationService
    {
        IConfiguration GetConfiguration();
    }
}