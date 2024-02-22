using System.Text;

namespace Akka.Pathfinder.DemoLayout;

public static class MapVisualizer
{
    public static string GenerateHtml(MapConfigWithPoints mapConfig)
    {
        return GenerateHtml(mapConfig, mapConfig.Width, mapConfig.Height, mapConfig.Depth);
    }

    public static string GenerateHtml(MapConfigWithPoints mapConfig, int width, int height, int depth)
    {
        var points = mapConfig.Configs.Values.SelectMany(x => x).ToDictionary(p => p.Id);
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<title>Map Visualization</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  html, body { height: 100%; margin: 0; padding: 0; overflow: hidden; }");
        sb.AppendLine("  body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; display: flex; flex-direction: column; }");
        sb.AppendLine("  h1 { margin: 15px 20px; flex-shrink: 0; font-size: 24px; }");
        sb.AppendLine("  .map-outer-container { background-color: #fff; padding: 10px; }");
        sb.AppendLine("  .grid { display: grid; gap: 1px; background-color: #eee; width: max-content; border: 1px solid #ddd; }");
        sb.AppendLine("  .cell { width: 10px; height: 10px; font-size: 6px; display: flex; align-items: center; justify-content: center; color: white; cursor: pointer; transition: transform 0.1s; background-color: #fff; }");
        sb.AppendLine("  .cell:hover { transform: scale(1.5); z-index: 10; outline: 1px solid #000; }");
        sb.AppendLine("  .layer-title { margin-top: 30px; border-bottom: 2px solid #333; padding-bottom: 5px; }");
        sb.AppendLine("  .legend { display: flex; gap: 15px; flex-wrap: wrap; margin-bottom: 20px; padding: 15px; background: #fff; border-radius: 8px; border: 1px solid #ccc; }");
        sb.AppendLine("  .legend-item { display: flex; align-items: center; gap: 8px; font-size: 14px; }");
        sb.AppendLine("  .legend-box { width: 18px; height: 18px; border: 1px solid #000; border-radius: 3px; }");
        sb.AppendLine("  .search-container { margin-bottom: 20px; position: sticky; top: 0; background: #f5f5f5; padding: 10px; z-index: 100; border-bottom: 1px solid #ccc; display: flex; flex-direction: column; gap: 5px; }");
        sb.AppendLine("  .search-input-wrapper { display: flex; gap: 10px; align-items: center; }");
        sb.AppendLine("  #search-input { padding: 8px; width: 300px; border-radius: 4px; border: 1px solid #ccc; font-size: 14px; }");
        sb.AppendLine("  #search-results { background: white; border: 1px solid #ccc; width: 300px; max-height: 300px; overflow-y: auto; box-shadow: 0 4px 6px rgba(0,0,0,0.1); border-radius: 0 0 4px 4px; display: none; }");
        sb.AppendLine("  .search-result-item { padding: 8px 12px; cursor: pointer; border-bottom: 1px solid #eee; font-size: 13px; color: #333; }");
        sb.AppendLine("  .search-result-item:hover { background-color: #e9ecef; }");
        sb.AppendLine("  .highlight { outline: 3px solid #ff0000 !important; transform: scale(3) !important; z-index: 1000 !important; transition: all 0.3s; }");
        sb.AppendLine("  .selection-info { margin-bottom: 20px; padding: 15px; background: #fff; border-radius: 8px; border: 1px solid #ccc; min-width: 300px; }");
        sb.AppendLine("  .selection-details-grid { display: grid; grid-template-columns: auto 1fr; gap: 5px 15px; font-size: 14px; }");
        sb.AppendLine("  .label { font-weight: bold; color: #666; }");
        sb.AppendLine("  .main-layout { display: flex; gap: 20px; flex: 1; overflow: hidden; padding: 0 20px 20px 20px; align-items: stretch; }");
        sb.AppendLine("  .controls-container { display: flex; flex-direction: column; gap: 10px; width: 350px; flex-shrink: 0; overflow-y: auto; padding-right: 10px; }");
        sb.AppendLine("  .map-content { flex: 1; overflow: auto; background-color: #fff; border: 2px solid #333; box-shadow: 0 4px 8px rgba(0,0,0,0.1); position: relative; }");
        sb.AppendLine("  .layer-switcher { display: flex; gap: 5px; flex-wrap: wrap; margin-bottom: 10px; padding: 15px; background: #fff; border-radius: 8px; border: 1px solid #ccc; }");
        sb.AppendLine("  .layer-btn { padding: 6px 12px; cursor: pointer; border: 1px solid #ccc; border-radius: 4px; background: #eee; font-size: 13px; }");
        sb.AppendLine("  .layer-btn.active { background: #3498db; color: white; border-color: #2980b9; }");
        sb.AppendLine("  .layer-container { display: none; }");
        sb.AppendLine("  .layer-container.active { display: block; }");
        sb.AppendLine("  .overlay-mode .map-content { position: relative; }");
        sb.AppendLine("  .overlay-mode .layer-container { display: block; position: absolute; top: 0; left: 0; opacity: 0.5; pointer-events: none; }");
        sb.AppendLine("  .overlay-mode .layer-container.active { opacity: 1; pointer-events: all; z-index: 5; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Map Visualization - Infrastructure</h1>");
        sb.AppendLine("<div class='main-layout'>");
        sb.AppendLine("  <div class='controls-container'>");
        sb.AppendLine("    <div class='layer-switcher' id='layer-controls'>");
        sb.AppendLine("      <h3 style='margin-top: 0; width: 100%;'>Layer Control</h3>");
        for (int i = 0; i < depth; i++)
        {
            var activeClass = i == 0 ? " active" : "";
            sb.AppendLine($"      <button class='layer-btn{activeClass}' onclick='showLayer({i}, this)'>Layer {i}</button>");
        }
        sb.AppendLine("      <div style='width: 100%; margin-top: 10px;'>");
        sb.AppendLine("        <input type='checkbox' id='overlay-toggle' onchange='toggleOverlay(this)'> <label for='overlay-toggle'>Overlay Mode</label>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class='search-container'>");
        sb.AppendLine("      <div class='search-input-wrapper'>");
        sb.AppendLine("        <input type='text' id='search-input' placeholder='Search station (e.g. Berlin, HH-Central, ID...)' onkeyup='searchPoint()' style='width: 100%; box-sizing: border-box;'>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div id='search-results' style='width: 100%; box-sizing: border-box;'></div>");
        sb.AppendLine("      <span id='search-count' style='font-size: 12px; color: #666; margin-top: 5px; display: block;'></span>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class='selection-info'>");
        sb.AppendLine("      <h3 style='margin-top: 0;'>Point Details</h3>");
        sb.AppendLine("      <div id='selection-details'>Click on a point to see details.</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class='legend' style='margin: 0;'>");
        foreach (var costKvp in TransportCosts.BaseCosts.OrderBy(x => x.Value))
        {
            var color = GetColorForCost(costKvp.Value);
            sb.AppendLine($"      <div class='legend-item'><div class='legend-box' style='background-color: {color};'></div><span>{costKvp.Key}</span></div>");
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class='map-content'>");

        for (int z = 0; z < depth; z++)
        {
            var activeClass = z == 0 ? " active" : "";
            sb.AppendLine($"<div id='layer-{z}' class='layer-container{activeClass}'>");
            sb.AppendLine($"<h2 class='layer-title' style='margin-top: 0;'>Layer Z = {z}</h2>");
            sb.AppendLine("<div class='map-outer-container'>");
            sb.AppendLine($"<div class='grid' style='grid-template-columns: repeat({width}, 10px);'>");

            var pointsInLayer = points.Values.Where(p => (p.Id / (width * height)) == z).ToList();
            foreach (var point in pointsInLayer)
            {
                int rem = point.Id % (width * height);
                int y = rem / width;
                int x = rem % width;

                string color = GetColorForCost(point.Cost);
                string tooltip = $"ID: {point.Id}, Pos: ({x},{y},{z})";
                string content = "";
                uint cost = point.Cost;
                uint connections = (uint)point.DirectionConfigs.Count;

                if (!string.IsNullOrEmpty(point.Name))
                {
                    tooltip += $"\nName: {point.Name}";
                }

                tooltip += $"\nCost: {point.Cost}\nConnections: {point.DirectionConfigs.Count}";

                if (point.Cost < 1000)
                {
                    if (point.Cost == TransportCosts.BaseCosts[TransportType.Station]) content = "S";
                    else if (point.Cost == TransportCosts.BaseCosts[TransportType.Terminal]) content = "T";
                    else if (point.Cost == TransportCosts.BaseCosts[TransportType.MetroTrack]) content = "M";
                    else if (point.Cost == TransportCosts.BaseCosts[TransportType.Depot]) content = "D";
                    else if (point.Cost == TransportCosts.BaseCosts[TransportType.FreightTrack]) content = "F";
                }

                string dataName = !string.IsNullOrEmpty(point.Name) ? point.Name : "";
                string dataType = TransportCosts.GetTypeFromCost(point.Cost).ToString();
                sb.AppendLine($"  <div class='cell' id='p-{point.Id}' data-id='{point.Id}' data-name='{dataName}' data-pos='{x},{y},{z}' data-cost='{cost}' data-type='{dataType}' data-connections='{connections}' style='grid-column: {x + 1}; grid-row: {y + 1}; background-color: {color};' title='{tooltip}' onclick='selectPoint(this)'>{content}</div>");
            }

            sb.AppendLine("</div>"); // grid
            sb.AppendLine("</div>"); // map-outer-container
            sb.AppendLine("</div>"); // layer-container
        }

        sb.AppendLine("  </div>"); // map-content
        sb.AppendLine("</div>"); // main-layout

        sb.AppendLine("<script>");
        sb.AppendLine("function showLayer(z, btn) {");
        sb.AppendLine("    document.querySelectorAll('.layer-container').forEach(c => c.classList.remove('active'));");
        sb.AppendLine("    document.getElementById('layer-' + z).classList.add('active');");
        sb.AppendLine("    document.querySelectorAll('.layer-btn').forEach(b => b.classList.remove('active'));");
        sb.AppendLine("    btn.classList.add('active');");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("function toggleOverlay(cb) {");
        sb.AppendLine("    if (cb.checked) {");
        sb.AppendLine("        document.querySelector('.main-layout').classList.add('overlay-mode');");
        sb.AppendLine("    } else {");
        sb.AppendLine("        document.querySelector('.main-layout').classList.remove('overlay-mode');");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("function selectPoint(element) {");
        sb.AppendLine("    document.querySelectorAll('.cell').forEach(c => c.classList.remove('highlight'));");
        sb.AppendLine("    element.classList.add('highlight');");
        sb.AppendLine("    ");
        sb.AppendLine("    let name = element.getAttribute('data-name');");
        sb.AppendLine("    let id = element.getAttribute('data-id');");
        sb.AppendLine("    let pos = element.getAttribute('data-pos');");
        sb.AppendLine("    let cost = element.getAttribute('data-cost');");
        sb.AppendLine("    let type = element.getAttribute('data-type');");
        sb.AppendLine("    let connections = element.getAttribute('data-connections');");
        sb.AppendLine("    ");
        sb.AppendLine("    let details = document.getElementById('selection-details');");
        sb.AppendLine("    details.innerHTML = `");
        sb.AppendLine("        <div class='selection-details-grid'>");
        sb.AppendLine("            <div class='label'>Name:</div><div>${name || '---'}</div>");
        sb.AppendLine("            <div class='label'>Type:</div><div>${type}</div>");
        sb.AppendLine("            <div class='label'>ID:</div><div>${id}</div>");
        sb.AppendLine("            <div class='label'>Position:</div><div>(${pos})</div>");
        sb.AppendLine("            <div class='label'>Cost:</div><div>${cost}</div>");
        sb.AppendLine("            <div class='label'>Connections:</div><div>${connections}</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    `;");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("function searchPoint() {");
        sb.AppendLine("    let input = document.getElementById('search-input').value.toLowerCase();");
        sb.AppendLine("    let results = document.getElementById('search-results');");
        sb.AppendLine("    let countSpan = document.getElementById('search-count');");
        sb.AppendLine("    results.innerHTML = '';");
        sb.AppendLine("    if (input.length < 2) { results.style.display = 'none'; countSpan.innerText = ''; return; }");
        sb.AppendLine("    let cells = document.querySelectorAll('.cell');");
        sb.AppendLine("    let matches = [];");
        sb.AppendLine("    cells.forEach(cell => {");
        sb.AppendLine("        let name = (cell.getAttribute('data-name') || '').toLowerCase();");
        sb.AppendLine("        let id = cell.getAttribute('data-id');");
        sb.AppendLine("        if ((name && name.includes(input)) || id === input) {");
        sb.AppendLine("            matches.push({");
        sb.AppendLine("                name: cell.getAttribute('data-name'),");
        sb.AppendLine("                id: id,");
        sb.AppendLine("                pos: cell.getAttribute('data-pos'),");
        sb.AppendLine("                element: cell");
        sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine("    });");
        sb.AppendLine("    countSpan.innerText = matches.length + ' matches';");
        sb.AppendLine("    if (matches.length > 0) {");
        sb.AppendLine("        results.style.display = 'block';");
        sb.AppendLine("        matches.slice(0, 20).forEach(match => {");
        sb.AppendLine("            let div = document.createElement('div');");
        sb.AppendLine("            div.className = 'search-result-item';");
        sb.AppendLine("            div.innerText = (match.name || 'ID: ' + match.id) + ' (Pos: ' + match.pos + ')';");
        sb.AppendLine("            div.onclick = () => {");
        sb.AppendLine("                selectPoint(match.element);");
        sb.AppendLine("                match.element.scrollIntoView({ behavior: 'smooth', block: 'center' });");
        sb.AppendLine("                results.style.display = 'none';");
        sb.AppendLine("            };");
        sb.AppendLine("            results.appendChild(div);");
        sb.AppendLine("        });");
        sb.AppendLine("    } else { results.style.display = 'none'; }");
        sb.AppendLine("}");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetColorForCost(uint cost)
    {
        return cost switch
        {
            0 => "#000000",      // Unknown/Free
            2 => "#e74c3c",      // ExpressTrack (Red)
            5 => "#333333",      // MainTrack (Dark Gray)
            6 => "#2ecc71",      // Junction (Green)
            8 => "#9b59b6",      // LocalTrack (Purple)
            10 => "#4a90e2",     // Station (Blue)
            20 => "#c0392b",     // Terminal (Dark Red)
            50 => "#34495e",     // Depot (Blue-Gray)
            100 => "#795548",    // MaintenanceArea (Brown)
            7 => "#8e44ad",      // MetroTrack (Dark Purple)
            14 => "#f39c12",     // TramTrack (Dark Orange)
            4 => "#2c3e50",      // FreightTrack (Midnight Blue)
            >= 1000 => "#eeeeee", // Empty (Light Gray)
            _ => "#7f8c8d"       // Others (Gray)
        };
    }

    public static void ExportToFile(MapConfigWithPoints mapConfig, string filePath)
    {
        var html = GenerateHtml(mapConfig);
        File.WriteAllText(filePath, html);
    }

    public static void ExportToFile(MapConfigWithPoints mapConfig, int width, int height, int depth, string filePath)
    {
        var html = GenerateHtml(mapConfig, width, height, depth);
        File.WriteAllText(filePath, html);
    }
}
