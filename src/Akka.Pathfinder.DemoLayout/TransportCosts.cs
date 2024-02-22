namespace Akka.Pathfinder.DemoLayout;

public static class TransportCosts
{
    public static readonly Dictionary<TransportType, uint> BaseCosts = new()
    {
        { TransportType.Empty, 1000 },
        { TransportType.Station, 10 },
        { TransportType.MainTrack, 5 },
        { TransportType.LocalTrack, 8 },
        { TransportType.ExpressTrack, 2 },
        { TransportType.Junction, 6 },
        { TransportType.Terminal, 20 },
        { TransportType.MaintenanceArea, 100 },
        { TransportType.Depot, 50 },
        { TransportType.MetroTrack, 9 },
        { TransportType.TramTrack, 14 },
        { TransportType.FreightTrack, 4 }
    };

    private static readonly Dictionary<uint, TransportType> _costToType = BaseCosts.ToDictionary(x => x.Value, x => x.Key);

    public static TransportType GetTypeFromCost(uint cost)
    {
        return _costToType.GetValueOrDefault(cost, TransportType.Empty);
    }
}