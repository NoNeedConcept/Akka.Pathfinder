using BoDi;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PointSteps
{
    private readonly ScenarioContext _context;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PointSteps>();

    public PointSteps(ScenarioContext context, ObjectContainer container)
    {
        _logger.Information("[TEST][PointSteps][ctor]", GetType().Name);
        _context = context;
    }
}
