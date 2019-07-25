using System;

namespace DemoBuilder.Services
{
    /// <summary>
    /// This service specify the environment name.
    /// Ex: if environment variable contain key/value for "ASPNETCORE_ENVIRONMENT", then environment variable values will be getting from appsettings.json file.
    /// Else getting from environment variables.
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        public EnvironmentService()
        {
            EnvironmentName = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AspnetCoreEnvironment)
                              ?? Constants.Environments.Production;
        }

        public string EnvironmentName { get; set; }
    }
}