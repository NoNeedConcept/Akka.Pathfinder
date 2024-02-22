using Microsoft.Extensions.Configuration;
using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Core;

public class ConfigurationSetupContainer : IConfigurationSetupContainer
{
    public void SetupConfiguration(IConfigurationManager builder)
    {
        builder.AddEnvironmentVariables();
    }
}