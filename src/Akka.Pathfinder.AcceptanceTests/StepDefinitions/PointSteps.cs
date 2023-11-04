using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PointSteps
{
    private readonly ScenarioContext _context;
    private readonly AkkaDriver _akkaDriver;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PointSteps>();

    public PointSteps(ScenarioContext context)
    {
        _logger.Information("[TEST][PointSteps][ctor]", GetType().Name);
        _context = context;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
    }
}
