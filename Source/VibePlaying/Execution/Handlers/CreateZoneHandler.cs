using System.Linq;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Creates stockpile or growing zones on the map.
    /// </summary>
    public class CreateZoneHandler : IActionHandler
    {
        public string ActionType => "create_zone";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("zone_type", out var zoneType);
            action.Params.TryGetValue("width", out var w);
            action.Params.TryGetValue("height", out var h);
            action.Params.TryGetValue("x", out var x);
            action.Params.TryGetValue("z", out var z);
            return $"Create {zoneType} zone {w}x{h} at ({x},{z})";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("zone_type", out var zoneType))
                return ActionResult.Fail("Missing zone_type");
            if (!action.Params.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out int x))
                return ActionResult.Fail("Missing or invalid x");
            if (!action.Params.TryGetValue("z", out var zStr) || !int.TryParse(zStr, out int z))
                return ActionResult.Fail("Missing or invalid z");

            int width = 5, height = 5;
            if (action.Params.TryGetValue("width", out var wStr) && int.TryParse(wStr, out int w))
                width = w;
            if (action.Params.TryGetValue("height", out var hStr) && int.TryParse(hStr, out int h))
                height = h;

            // Clamp size for safety
            width = UnityEngine.Mathf.Clamp(width, 1, 20);
            height = UnityEngine.Mathf.Clamp(height, 1, 20);

            // Collect valid, unoccupied cells
            var cells = new System.Collections.Generic.List<IntVec3>();
            for (int dx = 0; dx < width; dx++)
            {
                for (int dz = 0; dz < height; dz++)
                {
                    var cell = new IntVec3(x + dx, 0, z + dz);
                    if (!cell.InBounds(map)) continue;
                    if (map.zoneManager.ZoneAt(cell) != null) continue;
                    if (!cell.Standable(map) && !cell.GetTerrain(map).passability.Equals(Traversability.Standable)) continue;
                    cells.Add(cell);
                }
            }

            if (cells.Count == 0)
                return ActionResult.Fail("No valid cells for zone placement");

            switch (zoneType.ToLower())
            {
                case "stockpile":
                    return CreateStockpile(map, cells);
                case "growing":
                    return CreateGrowingZone(map, cells, action);
                default:
                    return ActionResult.Fail($"Unknown zone_type: {zoneType}");
            }
        }

        private static ActionResult CreateStockpile(Map map, System.Collections.Generic.List<IntVec3> cells)
        {
            var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
            map.zoneManager.RegisterZone(zone);
            foreach (var cell in cells)
                zone.AddCell(cell);
            return ActionResult.Ok($"Created stockpile zone with {cells.Count} cells");
        }

        private static ActionResult CreateGrowingZone(Map map, System.Collections.Generic.List<IntVec3> cells, ProposedAction action)
        {
            var zone = new Zone_Growing(map.zoneManager);
            map.zoneManager.RegisterZone(zone);
            foreach (var cell in cells)
                zone.AddCell(cell);

            // Set plant if specified
            if (action.Params.TryGetValue("plant_def", out var plantDefName) && !string.IsNullOrEmpty(plantDefName))
            {
                var plantDef = DefDatabase<ThingDef>.GetNamedSilentFail(plantDefName);
                if (plantDef != null)
                    zone.SetPlantDefToGrow(plantDef);
            }

            return ActionResult.Ok($"Created growing zone with {cells.Count} cells");
        }
    }
}
