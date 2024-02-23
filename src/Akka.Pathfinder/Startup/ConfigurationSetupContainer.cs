using Servus.Core.Application.Startup;

namespace Akka.Pathfinder.Startup;

public class ConfigurationSetupContaine : IConfigurationSetupContainer
{
    public void SetupConfiguration(IConfigurationManager builder)
    {
        builder.AddEnvironmentVariables();
    }
}