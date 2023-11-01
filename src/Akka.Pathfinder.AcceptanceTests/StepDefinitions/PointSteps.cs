using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests;

[Binding]
public class PointSteps
{
    private readonly ScenarioContext _context;
    private readonly PointConfigDriver _pointConfigDriver;
    private readonly AkkaDriver _akkaDriver;

    public PointSteps(ScenarioContext context)
    {
        Log.Information("[TEST][PointSteps][ctor]", GetType().Name);
        _context = context;
        _pointConfigDriver = EnvironmentSetupHooks.PointConfigDriver;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
    }
}
