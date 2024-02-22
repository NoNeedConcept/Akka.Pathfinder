namespace Akka.Pathfinder.DemoLayout;

public class GermanRailwayNetworkFactory : IMapFactory
{
    private int _width;
    private int _height;
    private int _depth = 1;
    private readonly Dictionary<int, TransportType> _networkMap = [];
    private readonly Dictionary<int, Dictionary<Directions, DirectionConfig>> _explicitConnections = [];
    private readonly List<Station> _stations = [];
    private readonly Guid _mapId;
    private GermanRailwayNetworkSettings _settings = new();

    public GermanRailwayNetworkFactory(Guid? mapId = null)
    {
        _mapId = mapId ?? Guid.NewGuid();
    }

    public MapConfigWithPoints Create(IMapSettings settings)
    {
        if (settings is not GermanRailwayNetworkSettings germanSettings)
        {
            throw new ArgumentException("Settings must be of type GermanRailwayNetworkSettings", nameof(settings));
        }

        _settings = germanSettings;

        var minScale = _settings.Detail == DetailLevel.Extreme ? 2 : 1;
        var scale = _settings.Scale < minScale ? minScale : _settings.Scale;

        var baseSize = _settings.Detail switch
        {
            DetailLevel.Low => 50,
            DetailLevel.High => 75,
            _ => 250
        };

        _width = baseSize * scale;
        _height = baseSize * scale;
        _depth = 2;

        _networkMap.Clear();
        _explicitConnections.Clear();
        _stations.Clear();

        var collectionId = Guid.NewGuid();

        var cityMap = new Dictionary<string, Station>();
        var cityScale = _settings.Detail switch
        {
            DetailLevel.Low => 0.4f,
            DetailLevel.High => 0.6f,
            _ => 1.6f
        };
        foreach (var city in GermanRailwayData.Cities)
        {
            if (_settings.Detail != DetailLevel.Extreme && city.Type != TransportType.Station &&
                city.Type != TransportType.Terminal)
            {
                continue;
            }

            cityMap[city.Name] = CreateStation((int)(city.X * scale * cityScale), (int)(city.Y * scale * cityScale), 1,
                city.Name, city.Type);
        }

        CreateMainLines(cityMap);

        if (germanSettings.IncludeRegionalLines)
        {
            CreateRegionalLines(cityMap);
        }

        if (germanSettings.IncludeMetro)
        {
            CreateMetroSystems(cityMap, scale, germanSettings.IncludeMetro);
        }

        if (germanSettings.IncludeTram)
        {
            CreateLocalTransit(cityMap, scale, germanSettings.IncludeTram);
        }

        CreateUserDefinedConnections(cityMap);

        AddDetailedInfrastructure();

        var totalPoints = _width * _height * _depth;
        var points = CreatePointsFromNetwork(totalPoints);

        EnsureFullConnectivity(points);

        CreateJunctions(points);

        SyncAllConnectionCosts(points);

        return new MapConfigWithPoints(
            _mapId,
            new Dictionary<Guid, List<PointConfig>> { { collectionId, points } },
            _width,
            _height,
            _depth
        );
    }

    private void SyncAllConnectionCosts(List<PointConfig> points)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var modified = false;
            var newDirections = new Dictionary<Directions, DirectionConfig>(point.DirectionConfigs);

            foreach (var dir in point.DirectionConfigs.Keys.ToList())
            {
                var conn = point.DirectionConfigs[dir];
                if (conn.TargetPointId < 0 || conn.TargetPointId >= points.Count) continue;
                var targetPoint = points[conn.TargetPointId];
                if (conn.Cost != targetPoint.Cost)
                {
                    newDirections[dir] = conn with { Cost = targetPoint.Cost };
                    modified = true;
                }
            }

            if (modified)
            {
                points[i] = point with { DirectionConfigs = newDirections };
            }
        }
    }

    private void CreateMainLines(Dictionary<string, Station> cities)
    {
        // North-South axes
        ConnectIfExist(cities, "Kiel", "Hamburg", TransportType.ExpressTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Westerland (Sylt)", "Hamburg", TransportType.MainTrack);
        ConnectIfExist(cities, "Flensburg", "Kiel", TransportType.MainTrack);

        // Hamburg - Hannover (Metronom + ICE)
        ConnectIfExist(cities, "Hamburg", "Hanover", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Hanover", "Kassel", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Kassel", "Fulda", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Fulda", "Frankfurt am Main", TransportType.ExpressTrack, TransportType.ExpressTrack);

        // Riedbahn + Main-Neckar (Frankfurt - Mannheim)
        ConnectIfExist(cities, "Frankfurt am Main", "Mannheim", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Mannheim", "Karlsruhe", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack);

        // Rhine Valley Railway
        ConnectIfExist(cities, "Karlsruhe", "Baden-Baden", TransportType.ExpressTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Baden-Baden", "Offenburg", TransportType.ExpressTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Offenburg", "Freiburg", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Freiburg", "Basel", TransportType.ExpressTrack, TransportType.MainTrack);

        // East axes
        ConnectIfExist(cities, "Stralsund", "Berlin", TransportType.MainTrack);
        ConnectIfExist(cities, "Rostock", "Berlin", TransportType.MainTrack);

        // Berlin - Hamburg (VDE)
        ConnectIfExist(cities, "Berlin", "Hamburg", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack);

        // VDE 8 (Berlin - Leipzig - Nuremberg - Munich)
        ConnectIfExist(cities, "Berlin", "Leipzig", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Leipzig", "Erfurt", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Erfurt", "Nuremberg", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Nuremberg", "Munich", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);

        // SFS Köln - Frankfurt
        ConnectIfExist(cities, "Cologne", "Siegburg/Bonn", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Siegburg/Bonn", "Montabaur", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Montabaur", "Limburg South", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Limburg South", "Frankfurt Airport", TransportType.ExpressTrack,
            TransportType.ExpressTrack);
        ConnectIfExist(cities, "Frankfurt Airport", "Frankfurt am Main", TransportType.ExpressTrack,
            TransportType.ExpressTrack);

        // Ruhrgebiet - Hannover - Berlin
        ConnectIfExist(cities, "Dortmund", "Hanover", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Hanover", "Berlin", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Berlin", "Frankfurt (Oder)", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Frankfurt am Main", "Saarbrücken", TransportType.ExpressTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Saarbrücken", "Mannheim", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Hamburg", "Bremen", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack);
        ConnectIfExist(cities, "Bremen", "Osnabrück", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Osnabrück", "Münster", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Münster", "Dortmund", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Frankfurt am Main", "Würzburg", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack);
        ConnectIfExist(cities, "Würzburg", "Nuremberg", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack);

        ConnectIfExist(cities, "Mannheim", "Stuttgart", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Stuttgart", "Ulm", TransportType.ExpressTrack, TransportType.ExpressTrack);
        ConnectIfExist(cities, "Ulm", "Augsburg", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack);
        ConnectIfExist(cities, "Augsburg", "Munich", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);
    }

    private void CreateRegionalLines(Dictionary<string, Station> cities)
    {
        ConnectIfExist(cities, "Hamburg", "Lübeck", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Hamburg", "Schwerin", TransportType.MainTrack);
        ConnectIfExist(cities, "Schwerin", "Rostock", TransportType.MainTrack);
        ConnectIfExist(cities, "Rostock", "Stralsund", TransportType.MainTrack);
        ConnectIfExist(cities, "Bremen", "Oldenburg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Oldenburg", "Emden", TransportType.MainTrack);
        ConnectIfExist(cities, "Hamburg", "Bergedorf", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Bergedorf", "Lüneburg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Lüneburg", "Uelzen", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Uelzen", "Celle", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Celle", "Hanover", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Kiel", "Neumünster", TransportType.MainTrack);
        ConnectIfExist(cities, "Neumünster", "Hamburg", TransportType.MainTrack);

        // Rhine-Ruhr Express (RRX) corridor
        ConnectIfExist(cities, "Dortmund", "Bochum", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Bochum", "Essen", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Essen", "Duisburg", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Duisburg", "Düsseldorf", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Düsseldorf", "Cologne", TransportType.ExpressTrack, TransportType.ExpressTrack,
            TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Cologne", "Bonn", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Bonn", "Koblenz", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Koblenz", "Mainz", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Mainz", "Mannheim", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Koblenz", "Trier", TransportType.MainTrack);
        ConnectIfExist(cities, "Trier", "Saarbrücken", TransportType.MainTrack);
        ConnectIfExist(cities, "Cologne", "Aachen", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Düsseldorf", "Wuppertal", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Wuppertal", "Dortmund", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Münster", "Bielefeld", TransportType.MainTrack);
        ConnectIfExist(cities, "Bielefeld", "Hanover", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Hanover", "Braunschweig", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Braunschweig", "Wolfsburg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Wolfsburg", "Magdeburg", TransportType.MainTrack, TransportType.MainTrack);

        ConnectIfExist(cities, "Berlin", "Potsdam", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Berlin", "Magdeburg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Magdeburg", "Halle (Saale)", TransportType.MainTrack);
        ConnectIfExist(cities, "Halle (Saale)", "Leipzig", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Leipzig", "Dresden", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Dresden", "Görlitz", TransportType.MainTrack);
        ConnectIfExist(cities, "Cottbus", "Görlitz", TransportType.MainTrack);
        ConnectIfExist(cities, "Berlin", "Cottbus", TransportType.MainTrack);
        ConnectIfExist(cities, "Dresden", "Chemnitz", TransportType.MainTrack);
        ConnectIfExist(cities, "Chemnitz", "Gera", TransportType.MainTrack);
        ConnectIfExist(cities, "Gera", "Erfurt", TransportType.MainTrack);
        ConnectIfExist(cities, "Eisenach", "Erfurt", TransportType.MainTrack);
        ConnectIfExist(cities, "Eisenach", "Fulda", TransportType.MainTrack);
        ConnectIfExist(cities, "Leipzig", "Chemnitz", TransportType.MainTrack);

        ConnectIfExist(cities, "Kassel", "Göttingen", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Göttingen", "Hannover", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Kassel", "Gießen", TransportType.MainTrack);
        ConnectIfExist(cities, "Gießen", "Frankfurt am Main", TransportType.MainTrack);
        ConnectIfExist(cities, "Frankfurt am Main", "Wiesbaden", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Frankfurt am Main", "Mainz", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Frankfurt am Main", "Darmstadt", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Frankfurt am Main", "Hanau", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Hanau", "Fulda", TransportType.MainTrack);
        ConnectIfExist(cities, "Würzburg", "Aschaffenburg", TransportType.MainTrack);
        ConnectIfExist(cities, "Aschaffenburg", "Hanau", TransportType.MainTrack);

        ConnectIfExist(cities, "Mannheim", "Heidelberg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Heidelberg", "Stuttgart", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Karlsruhe", "Pforzheim", TransportType.MainTrack);
        ConnectIfExist(cities, "Pforzheim", "Stuttgart", TransportType.MainTrack);
        ConnectIfExist(cities, "Stuttgart", "Heilbronn", TransportType.MainTrack);
        ConnectIfExist(cities, "Heilbronn", "Würzburg", TransportType.MainTrack);
        ConnectIfExist(cities, "Stuttgart", "Tübingen", TransportType.MainTrack);
        ConnectIfExist(cities, "Ulm", "Augsburg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Ulm", "Lindau", TransportType.MainTrack);
        ConnectIfExist(cities, "Augsburg", "Ingolstadt", TransportType.MainTrack);
        ConnectIfExist(cities, "Ingolstadt", "Nuremberg", TransportType.MainTrack);
        ConnectIfExist(cities, "Nuremberg", "Regensburg", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Nuremberg", "Bayreuth", TransportType.MainTrack);
        ConnectIfExist(cities, "Regensburg", "Passau", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Munich", "Rosenheim", TransportType.MainTrack, TransportType.MainTrack);
        ConnectIfExist(cities, "Munich", "Garmisch-Partenkirchen", TransportType.MainTrack);
        ConnectIfExist(cities, "Munich", "Oberstdorf", TransportType.MainTrack);
        ConnectIfExist(cities, "Garmisch-Partenkirchen", "Oberstdorf", TransportType.MainTrack);
    }

    private void CreateMetroSystems(Dictionary<string, Station> cities, int scale, bool includeMetro)
    {
        if (includeMetro)
        {
            string[] hubs =
            [
                "Berlin", "Hamburg", "Munich", "Cologne", "Frankfurt am Main", "Stuttgart", "Nuremberg", "Leipzig",
                "Hanover"
            ];
            foreach (var hubName in hubs)
            {
                if (!cities.TryGetValue(hubName, out var hub)) continue;
                var metroHub = GetOrCreateStationAtLevel(hub, 0);
                var branches = hubName is "Berlin" or "Hamburg" or "Munich" ? 8 : 4;
                var length = 12 * scale;

                branches += 4;
                length *= 2;

                GenerateStarNetwork(metroHub, TransportType.MetroTrack, branches, length);
            }
        }

        if (includeMetro)
        {
            string[] ruhrCities = ["Dortmund", "Essen", "Duisburg", "Düsseldorf", "Bochum"];
            foreach (var cityName in ruhrCities)
            {
                if (cities.TryGetValue(cityName, out var city))
                {
                    var metroHub = GetOrCreateStationAtLevel(city, 0);
                    const int branches = 6;
                    var length = 12 * scale;

                    GenerateStarNetwork(metroHub, TransportType.MetroTrack, branches, length);
                }
            }

            if (cities.TryGetValue("Berlin", out var berlin))
            {
                GenerateStarNetwork(GetOrCreateStationAtLevel(berlin, 0), TransportType.MetroTrack, 8, 8 * scale);
            }

            if (cities.TryGetValue("Hamburg", out var hamburg))
            {
                GenerateStarNetwork(GetOrCreateStationAtLevel(hamburg, 0), TransportType.MetroTrack, 6, 7 * scale);
            }

            if (cities.TryGetValue("Munich", out var muenchen))
            {
                GenerateStarNetwork(GetOrCreateStationAtLevel(muenchen, 0), TransportType.MetroTrack, 6, 7 * scale);
            }

            if (cities.TryGetValue("Frankfurt am Main", out var ffm))
            {
                GenerateStarNetwork(GetOrCreateStationAtLevel(ffm, 0), TransportType.MetroTrack, 5, 6 * scale);
            }

            if (cities.TryGetValue("Nuremberg", out var nue))
            {
                GenerateStarNetwork(GetOrCreateStationAtLevel(nue, 0), TransportType.MetroTrack, 3, 5 * scale);
            }
        }
    }

    private void CreateLocalTransit(Dictionary<string, Station> cities, int scale, bool includeTram)
    {
        if (!includeTram) return;

        string[] ruhrCities = ["Dortmund", "Essen", "Duisburg", "Düsseldorf", "Bochum"];
        foreach (var cityName in ruhrCities)
        {
            if (cities.TryGetValue(cityName, out var city))
            {
                const int branches = 8;
                var length = 8 * scale;

                GenerateStarNetwork(city, TransportType.TramTrack, branches, length);
            }
        }
    }

    private void AddDetailedInfrastructure()
    {
        if (_settings.Detail == DetailLevel.Low) return;

        var importantStations = _stations.Where(s =>
            s.Type is TransportType.Terminal or TransportType.Station).ToList();

        foreach (var station in importantStations)
        {
            if (station.Name.StartsWith("Stop-")) continue;

            var searchName = station.Name.Replace(" (underground)", "");
            var cityData = GermanRailwayData.Cities.FirstOrDefault(c => c.Name == searchName);
            if (cityData == null)
            {
                var code = GermanRailwayData.CodeToName.FirstOrDefault(x => x.Value == searchName).Key;
                if (code != null) cityData = GermanRailwayData.Cities.FirstOrDefault(c => c.Name == code);
            }

            var platformTarget = cityData != null
                ? station.Z == 1 ? cityData.Platforms : cityData.UndergroundPlatforms
                : station.Type == TransportType.Station
                    ? 2
                    : 0;

            if (_settings.Detail == DetailLevel.High)
            {
                platformTarget = Math.Max(1, platformTarget / 2);
            }

            if (platformTarget <= 0) continue;

            var platformsPlaced = 0;
            const int maxRadius = 20;

            for (var r = 1; r <= maxRadius && platformsPlaced < platformTarget; r++)
            {
                for (var i = -r; i <= r && platformsPlaced < platformTarget; i++)
                {
                    for (var j = -r; j <= r && platformsPlaced < platformTarget; j++)
                    {
                        if (Math.Abs(i) < r && Math.Abs(j) < r) continue;

                        var px = station.X + i;
                        var py = station.Y + j;

                        if (px < 0 || px >= _width || py < 0 || py >= _height) continue;

                        var id = GetId(px, py, station.Z);
                        if (_networkMap.ContainsKey(id)) continue;
                        var platform = CreateStation(px, py, station.Z,
                            $"Track-{platformsPlaced + 1}-{station.Name}", TransportType.Station);
                        ConnectStations(station, platform, TransportType.LocalTrack, true);
                        platformsPlaced++;
                    }
                }
            }
        }

        if (_settings.Detail != DetailLevel.Extreme) return;

        var terminals = _stations.Where(s => s.Type == TransportType.Terminal).ToList();
        foreach (var terminal in terminals)
        {
            if (HasConnectedDepot(terminal)) continue;

            var depotsPlacedCount = 0;
            const int depotsToPlace = 1; // Always at most one depot per station

            for (var r = 4; r <= 20 && depotsPlacedCount < depotsToPlace; r++)
            {
                for (var ox = -r; ox <= r && depotsPlacedCount < depotsToPlace; ox++)
                {
                    for (var oy = -r; oy <= r && depotsPlacedCount < depotsToPlace; oy++)
                    {
                        if (Math.Abs(ox) < r && Math.Abs(oy) < r) continue;
                        var dx = terminal.X + ox;
                        var dy = terminal.Y + oy;

                        if (dx < 1 || dx >= _width - 1 || dy < 1 || dy >= _height - 1) continue;

                        var id = GetId(dx, dy, terminal.Z);

                        if (!_networkMap.ContainsKey(id))
                        {
                            const int areaRadius = 2;
                            var areaFree = true;
                            for (var mi = -areaRadius; mi <= areaRadius; mi++)
                            {
                                for (var mj = -areaRadius; mj <= areaRadius; mj++)
                                {
                                    if (!_networkMap.ContainsKey(GetId(dx + mi, dy + mj, terminal.Z))) continue;
                                    areaFree = false;
                                    break;
                                }

                                if (!areaFree) break;
                            }

                            if (areaFree)
                            {
                                var depot = CreateStation(dx, dy, terminal.Z,
                                    $"Depot-{depotsPlacedCount + 1}-{terminal.Name}", TransportType.Depot);
                                ConnectStations(terminal, depot, TransportType.MaintenanceArea);

                                for (var i = -areaRadius; i <= areaRadius; i++)
                                {
                                    for (var j = -areaRadius; j <= areaRadius; j++)
                                    {
                                        if (i == 0 && j == 0) continue;
                                        var curX = dx + i;
                                        var curY = dy + j;
                                        ApplyTrackAt(curX, curY, terminal.Z, TransportType.MaintenanceArea);

                                        // Connect to neighbor to avoid isolated points that trigger EnsureFullConnectivity
                                        if (i > -areaRadius)
                                            AddExplicitConnection(curX - 1, curY, terminal.Z, curX, curY, terminal.Z);
                                        if (j > -areaRadius)
                                            AddExplicitConnection(curX, curY - 1, terminal.Z, curX, curY, terminal.Z);
                                    }
                                }

                                depotsPlacedCount++;
                            }
                        }
                    }
                }
            }
        }
    }

    private bool HasConnectedDepot(Station station)
    {
        var startId = GetId(station.X, station.Y, station.Z);
        if (!_explicitConnections.TryGetValue(startId, out var conns)) return false;

        var visited = new HashSet<int> { startId };
        var queue = new Queue<int>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (_networkMap.TryGetValue(currentId, out var type) && type == TransportType.Depot)
                return true;

            if (_explicitConnections.TryGetValue(currentId, out var neighbors))
            {
                foreach (var conn in neighbors.Values)
                {
                    if (visited.Contains(conn.TargetPointId)) continue;

                    var neighborType = _networkMap.GetValueOrDefault(conn.TargetPointId, TransportType.Empty);
                    // We only follow MaintenanceArea and Depot to find connected depots
                    if (neighborType == TransportType.MaintenanceArea || neighborType == TransportType.Depot)
                    {
                        visited.Add(conn.TargetPointId);
                        queue.Enqueue(conn.TargetPointId);
                    }
                }
            }
        }

        return false;
    }

    private void GenerateStarNetwork(Station center, TransportType type, int branches, int length)
    {
        for (var i = 0; i < branches; i++)
        {
            var angle = 2 * Math.PI / branches * i;
            var endX = center.X + (int)(Math.Cos(angle) * length);
            var endY = center.Y + (int)(Math.Sin(angle) * length);

            var endStation = CreateStation(endX, endY, center.Z, $"Stop-{type}-{center.Name}-{i}",
                TransportType.Station);
            ConnectStations(center, endStation, type);
        }
    }

    private Station GetOrCreateStationAtLevel(Station surfaceStation, int z)
    {
        if (z == surfaceStation.Z) return surfaceStation;

        var existing = _stations.FirstOrDefault(s => s.X == surfaceStation.X && s.Y == surfaceStation.Y && s.Z == z);
        if (existing != null) return existing;

        var name = z == 0 ? $"{surfaceStation.Name} (underground)" : surfaceStation.Name;
        var deepStation = CreateStation(surfaceStation.X, surfaceStation.Y, z, name,
            TransportType.Station);
        ConnectStations(surfaceStation, deepStation, TransportType.Station);
        return deepStation;
    }

    private static bool IsUnderground(TransportType type)
    {
        return type is TransportType.MetroTrack;
    }

    private void ConnectIfExist(Dictionary<string, Station> cities, string from, string to,
        params TransportType[] trackTypes)
    {
        if (cities.TryGetValue(from, out var s1) && cities.TryGetValue(to, out var s2))
        {
            if (trackTypes.Length == 0) return;

            var tracks = trackTypes.ToList();

            if (_settings.Detail == DetailLevel.Low)
            {
                // In Low detail, only take the first track type and only one track
                tracks = [tracks[0]];
            }
            else if (_settings.Detail == DetailLevel.Extreme && tracks.Count >= 2)
            {
                // In Extreme detail, we add one extra capacity track if there are at least 2 tracks
                var mostCommon = tracks.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key;
                tracks.Add(mostCommon);
            }

            var targetZ = IsUnderground(tracks[0]) ? 0 : 1;
            s1 = GetOrCreateStationAtLevel(s1, targetZ);
            s2 = GetOrCreateStationAtLevel(s2, targetZ);

            if (tracks.Count == 1)
            {
                ConnectStations(s1, s2, tracks[0]);
                return;
            }

            var isMostlyHorizontal = Math.Abs(s2.X - s1.X) > Math.Abs(s2.Y - s1.Y);

            for (var i = 0; i < tracks.Count; i++)
            {
                var offset = i - tracks.Count / 2;
                if (offset == 0)
                {
                    ConnectStations(s1, s2, tracks[i], false);
                    continue;
                }

                var offX = isMostlyHorizontal ? 0 : offset;
                var offY = isMostlyHorizontal ? offset : 0;

                var startSide = new Station(s1.X + offX, s1.Y + offY, s1.Z, "tmp", tracks[i]);
                var endSide = new Station(s2.X + offX, s2.Y + offY, s2.Z, "tmp", tracks[i]);

                ConnectStations(startSide, endSide, tracks[i], i % 2 == 1);
                ConnectStations(s1, startSide, tracks[i], i % 2 == 0);
                ConnectStations(s2, endSide, tracks[i], i % 2 == 1);
            }
        }
    }

    /// <summary>
    /// Ensures all non-empty points are connected. If not, adds connections.
    /// </summary>
    private void EnsureFullConnectivity(List<PointConfig> points)
    {
        var components = FindDisconnectedComponents(points);

        if (components.Count <= 1) return;
        var largestComponent = components.OrderByDescending(c => c.Count).First();

        foreach (var component in components.Where(component => component != largestComponent))
        {
            ConnectComponents(points, component, largestComponent);
        }
    }

    /// <summary>
    /// Finds all disconnected components in the network
    /// </summary>
    private List<HashSet<int>> FindDisconnectedComponents(List<PointConfig> points)
    {
        var visited = new HashSet<int>();
        var components = new List<HashSet<int>>();

        for (var i = 0; i < points.Count; i++)
        {
            // Skip empty points
            if (points[i].DirectionConfigs.Count == 0 && points[i].Cost >= 1000)
                continue;

            if (visited.Contains(i)) continue;
            var component = GetConnectedComponent(points, i);
            components.Add(component);
            visited.UnionWith(component);
        }

        return components;
    }

    /// <summary>
    /// Gets all points in the same connected component as the start point
    /// </summary>
    private HashSet<int> GetConnectedComponent(List<PointConfig> points, int start)
    {
        var component = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(start);
        component.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in points[current].DirectionConfigs.Values
                         .Where(neighbor => !component.Contains(neighbor.TargetPointId)))
            {
                component.Add(neighbor.TargetPointId);
                queue.Enqueue(neighbor.TargetPointId);
            }
        }

        return component;
    }

    /// <summary>
    /// Connects two components by finding the closest points and adding a path
    /// </summary>
    private void ConnectComponents(List<PointConfig> points, HashSet<int> component1, HashSet<int> component2)
    {
        int bestFrom = -1, bestTo = -1;
        var minDistance = double.MaxValue;

        foreach (var id1 in component1)
        {
            var (x1, y1, z1) = IdToCoordinates(id1);

            foreach (var id2 in component2)
            {
                var (x2, y2, z2) = IdToCoordinates(id2);
                var distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));

                if (distance >= minDistance) continue;
                minDistance = distance;
                bestFrom = id1;
                bestTo = id2;
            }
        }

        if (bestFrom != -1 && bestTo != -1)
        {
            var (x1, y1, z1) = IdToCoordinates(bestFrom);
            var (x2, y2, z2) = IdToCoordinates(bestTo);

            var trackType = z1 == 0 && z2 == 0 ? TransportType.MetroTrack :
                z1 == 1 && z2 == 1 ? TransportType.MainTrack : TransportType.Station;
            DrawConnectionLine(x1, y1, z1, x2, y2, z2, trackType);
            RebuildConnectionsForArea(points, x1, y1, z1, x2, y2, z2);
        }
    }

    /// <summary>
    /// Draws a connection line between two points in the network map
    /// </summary>
    private void DrawConnectionLine(int x1, int y1, int z1, int x2, int y2, int z2, TransportType trackType)
    {
        var s1 = new Station(x1, y1, z1, "tmp", trackType);
        var s2 = new Station(x2, y2, z2, "tmp", trackType);
        ConnectStations(s1, s2, trackType);
    }

    /// <summary>
    /// Rebuilds connections for points in the affected area
    /// </summary>
    private void RebuildConnectionsForArea(List<PointConfig> points, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        var minX = Math.Max(0, Math.Min(x1, x2) - 1);
        var maxX = Math.Min(_width - 1, Math.Max(x1, x2) + 1);
        var minY = Math.Max(0, Math.Min(y1, y2) - 1);
        var maxY = Math.Min(_height - 1, Math.Max(y1, y2) + 1);
        var minZ = Math.Max(0, Math.Min(z1, z2) - 1);
        var maxZ = Math.Min(_depth - 1, Math.Max(z1, z2) + 1);

        for (var z = minZ; z <= maxZ; z++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var pointId = GetId(x, y, z);
                    if (pointId >= 0 && pointId < points.Count)
                    {
                        UpdatePointConnections(points, pointId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates connections for a single point based on current network map
    /// </summary>
    private void UpdatePointConnections(List<PointConfig> points, int pointId)
    {
        var point = points[pointId];
        var newDirections = _explicitConnections.GetValueOrDefault(pointId) ?? [];
        var transportType = _networkMap.GetValueOrDefault(pointId, TransportType.Empty);
        var newCost = TransportCosts.BaseCosts[transportType];

        points[pointId] = point with { DirectionConfigs = newDirections, Cost = newCost };
    }

    /// <summary>
    /// Converts point ID to x,y,z coordinates
    /// </summary>
    private (int x, int y, int z) IdToCoordinates(int id)
    {
        var z = id / (_width * _height);
        var rem = id % (_width * _height);
        var y = rem / _width;
        var x = rem % _width;
        return (x, y, z);
    }

    private Station CreateStation(int x, int y, int z, string name, TransportType type)
    {
        x = Math.Clamp(x, 0, _width - 1);
        y = Math.Clamp(y, 0, _height - 1);
        z = Math.Clamp(z, 0, _depth - 1);

        var station = new Station(x, y, z, name, type);
        _stations.Add(station);
        _networkMap[GetId(x, y, z)] = type;

        return station;
    }

    private void ConnectStations(Station from, Station to, TransportType trackType, bool preferYFirst = false)
    {
        int x = from.X, y = from.Y, z = from.Z;
        int targetX = to.X, targetY = to.Y, targetZ = to.Z;

        ApplyTrackAt(x, y, z, trackType);

        while (x != targetX || y != targetY || z != targetZ)
        {
            int lastX = x, lastY = y, lastZ = z;

            if (preferYFirst)
            {
                if (y != targetY) y += targetY > y ? 1 : -1;
                else if (x != targetX) x += targetX > x ? 1 : -1;
                else if (z != targetZ) z += targetZ > z ? 1 : -1;
            }
            else
            {
                if (x != targetX) x += targetX > x ? 1 : -1;
                else if (y != targetY) y += targetY > y ? 1 : -1;
                else if (z != targetZ) z += targetZ > z ? 1 : -1;
            }

            ApplyTrackAt(x, y, z, trackType);
            AddExplicitConnection(lastX, lastY, lastZ, x, y, z);
        }
    }

    private void ApplyTrackAt(int x, int y, int z, TransportType trackType)
    {
        var id = GetId(x, y, z);
        var currentType = _networkMap.GetValueOrDefault(id, TransportType.Empty);

        if (currentType == TransportType.Empty)
        {
            _networkMap[id] = trackType;
        }
        else if (IsTrackType(currentType) && IsTrackType(trackType))
        {
            if (TransportCosts.BaseCosts[trackType] < TransportCosts.BaseCosts[currentType])
            {
                _networkMap[id] = trackType;
            }
        }
    }

    private void CreateJunctions(List<PointConfig>? points = null)
    {
        var pointIds = _networkMap.Keys.ToList();
        foreach (var id in pointIds)
        {
            var currentType = _networkMap[id];

            if (!IsTrackType(currentType)) continue;

            var (x, y, z) = IdToCoordinates(id);
            if (CountConnections(x, y, z) >= 3)
            {
                _networkMap[id] = TransportType.Junction;

                if (points != null && id < points.Count)
                {
                    var cost = TransportCosts.BaseCosts[TransportType.Junction];
                    points[id] = points[id] with { Cost = cost };
                }
            }
        }
    }

    private int CountConnections(int x, int y, int z)
    {
        var id = GetId(x, y, z);
        var connections = _explicitConnections.GetValueOrDefault(id);
        return connections?.Count ?? 0;
    }

    private static bool IsTrackType(TransportType type)
    {
        return type is TransportType.MainTrack or TransportType.LocalTrack or TransportType.ExpressTrack
            or TransportType.MetroTrack or TransportType.TramTrack
            or TransportType.FreightTrack;
    }

    private void AddExplicitConnection(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        if (x1 < 0 || x1 >= _width || y1 < 0 || y1 >= _height || z1 < 0 || z1 >= _depth ||
            x2 < 0 || x2 >= _width || y2 < 0 || y2 >= _height || z2 < 0 || z2 >= _depth)
            return;

        var id1 = GetId(x1, y1, z1);
        var id2 = GetId(x2, y2, z2);

        var type2 = _networkMap.GetValueOrDefault(id2, TransportType.Empty);
        var cost2 = TransportCosts.BaseCosts[type2];

        var dir12 = Directions.None;
        var dir21 = Directions.None;

        if (x2 > x1)
        {
            dir12 = Directions.Right;
            dir21 = Directions.Left;
        }
        else if (x2 < x1)
        {
            dir12 = Directions.Left;
            dir21 = Directions.Right;
        }
        else if (y2 > y1)
        {
            dir12 = Directions.Bottom;
            dir21 = Directions.Top;
        }
        else if (y2 < y1)
        {
            dir12 = Directions.Top;
            dir21 = Directions.Bottom;
        }
        else if (z2 > z1)
        {
            dir12 = Directions.Back;
            dir21 = Directions.Front;
        }
        else if (z2 < z1)
        {
            dir12 = Directions.Front;
            dir21 = Directions.Back;
        }

        if (dir12 == Directions.None) return;

        if (!_explicitConnections.TryGetValue(id1, out var connections1))
        {
            connections1 = [];
            _explicitConnections[id1] = connections1;
        }

        connections1[dir12] = new DirectionConfig(id2, cost2);

        var type1 = _networkMap.GetValueOrDefault(id1, TransportType.Empty);
        var cost1 = TransportCosts.BaseCosts[type1];
        if (!_explicitConnections.TryGetValue(id2, out var connections2))
        {
            connections2 = [];
            _explicitConnections[id2] = connections2;
        }

        connections2[dir21] = new DirectionConfig(id1, cost1);
    }

    private List<PointConfig> CreatePointsFromNetwork(int pointCount)
    {
        var points = new List<PointConfig>(pointCount);
        var stationNames = _stations.ToLookup(s => GetId(s.X, s.Y, s.Z), s => s.Name)
            .ToDictionary(l => l.Key, l => string.Join(", ", l.Distinct()));

        for (var i = 0; i < pointCount; i++)
        {
            var transportType = _networkMap.GetValueOrDefault(i, TransportType.Empty);
            var cost = TransportCosts.BaseCosts[transportType];
            var directionConfigs = _explicitConnections.GetValueOrDefault(i) ?? [];
            var name = stationNames.GetValueOrDefault(i);

            points.Add(new PointConfig(
                Id: i,
                Cost: cost,
                DirectionConfigs: directionConfigs,
                HasChanges: false,
                Name: name
            ));
        }

        return points;
    }

    private void CreateUserDefinedConnections(Dictionary<string, Station> cities)
    {
        void AddConn(string fromCode, string toCode, TransportType type)
        {
            var fromName = GermanRailwayData.CodeToName.GetValueOrDefault(fromCode, fromCode);
            var toName = GermanRailwayData.CodeToName.GetValueOrDefault(toCode, toCode);
            ConnectIfExist(cities, fromName, toName, type);
        }

        // Metro connections in Berlin
        AddConn("B-Central", "B-East", TransportType.MetroTrack);
        AddConn("B-Central", "B-South", TransportType.MetroTrack);
        AddConn("B-Central", "B-Spandau", TransportType.MetroTrack);
        AddConn("B-South", "B-East", TransportType.MetroTrack);

        // Metro in Hamburg
        AddConn("HH-Central", "HH-Altona", TransportType.MetroTrack);
        AddConn("HH-Central", "HH-Harburg", TransportType.MetroTrack);

        // Metro in Munich
        AddConn("M-Central", "M-East", TransportType.MetroTrack);
        AddConn("M-Central", "M-Pasing", TransportType.MetroTrack);

        // Metro in Frankfurt
        AddConn("F-Central", "F-South", TransportType.MetroTrack);

        // Freight traffic
        AddConn("HH-Harburg", "HH-Freight-Maschen", TransportType.FreightTrack);
        AddConn("HH-Freight-Maschen", "Maschen-Yard", TransportType.FreightTrack);
        AddConn("K-Central", "K-Freight-Eifeltor", TransportType.FreightTrack);
        AddConn("M-Central", "M-Freight-North", TransportType.FreightTrack);
        AddConn("M-Freight-North", "Muenchen-North-Yard", TransportType.FreightTrack);
        AddConn("F-Central", "F-Freight", TransportType.FreightTrack);
        AddConn("N-Central", "N-Freight", TransportType.FreightTrack);
        AddConn("N-Freight", "Nuernberg-Yard", TransportType.FreightTrack);
        AddConn("HB-Central", "HB-Freight", TransportType.FreightTrack);
        AddConn("DO-Central", "DO-Freight", TransportType.FreightTrack);
        AddConn("DU-Central", "DU-Freight", TransportType.FreightTrack);
        AddConn("L-Central", "L-Freight", TransportType.FreightTrack);
        AddConn("B-East", "B-Freight-South", TransportType.FreightTrack);
        AddConn("MA-Central", "MA-Freight", TransportType.FreightTrack);
        AddConn("MA-Freight", "Mannheim-Yard", TransportType.FreightTrack);
        AddConn("KA-Central", "KA-Freight", TransportType.FreightTrack);
        AddConn("S-Central", "S-Freight", TransportType.FreightTrack);
        AddConn("S-Freight", "Kornwestheim-Yard", TransportType.FreightTrack);
        AddConn("BR-Central", "BR-Freight", TransportType.FreightTrack);
        AddConn("HH-Altona", "HH-Freight-Altona", TransportType.FreightTrack);

        AddConn("DO-Freight", "Hagen-Vorhalle-Yard", TransportType.FreightTrack);
        AddConn("Hagen-Vorhalle-Yard", "Hamm-Yard", TransportType.FreightTrack);
        AddConn("B-Spandau", "Seddin-Yard", TransportType.FreightTrack);

        // Depots
        AddConn("M-Central", "M-Depot", TransportType.MaintenanceArea);
        AddConn("HH-Central", "HH-Depot-Eidelstedt", TransportType.MaintenanceArea);
        AddConn("B-East", "B-Depot-Rummelsburg", TransportType.MaintenanceArea);
        AddConn("K-Central", "K-Depot", TransportType.MaintenanceArea);
        AddConn("F-Central", "F-Depot", TransportType.MaintenanceArea);
        AddConn("N-Central", "N-Depot", TransportType.MaintenanceArea);
        AddConn("L-Central", "L-Depot", TransportType.MaintenanceArea);
        AddConn("S-Central", "S-Depot", TransportType.MaintenanceArea);
        AddConn("HB-Central", "HB-Depot", TransportType.MaintenanceArea);
        AddConn("DO-Central", "DO-Depot", TransportType.MaintenanceArea);
        AddConn("DD-Central", "DD-Depot", TransportType.MaintenanceArea);
        AddConn("ER-Central", "ER-Depot", TransportType.MaintenanceArea);

        // Missing regional connections
        AddConn("BR-Central", "OL-Central", TransportType.MainTrack);
        AddConn("OL-Central", "OS-Central", TransportType.MainTrack);
        AddConn("OS-Central", "MS-Central", TransportType.MainTrack);
    }

    private int GetId(int x, int y, int z)
    {
        return z * _width * _height + y * _width + x;
    }
}