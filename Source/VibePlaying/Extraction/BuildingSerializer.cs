using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace VibePlaying
{
    public static class BuildingSerializer
    {
        public static void Append(StringBuilder sb, Map map)
        {
            sb.Append("\"buildings\":{");

            // Aggregate buildings by type
            var buildingCounts = new Dictionary<string, int>();
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                var defName = building.def.defName;
                if (buildingCounts.ContainsKey(defName))
                    buildingCounts[defName]++;
                else
                    buildingCounts[defName] = 1;
            }

            // Sort by count descending, take top 30
            sb.Append("\"counts\":{");
            var sorted = buildingCounts.OrderByDescending(kv => kv.Value).Take(30);
            bool first = true;
            foreach (var kv in sorted)
            {
                if (!first) sb.Append(',');
                sb.Append($"\"{kv.Key}\":{kv.Value}");
                first = false;
            }
            sb.Append("},");

            // Rooms summary (bedrooms, hospitals, kitchens, etc.)
            sb.Append("\"rooms\":[");
            var rooms = map.regionGrid.allRooms
                .Where(r => !r.TouchesMapEdge && !r.IsDoorway)
                .Take(20);

            first = true;
            foreach (var room in rooms)
            {
                if (!first) sb.Append(',');
                var role = room.Role?.defName ?? "None";
                var impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
                sb.Append($"{{\"role\":\"{role}\",\"size\":{room.CellCount},\"impressiveness\":{impressiveness:F1}}}");
                first = false;
            }
            sb.Append(']');

            sb.Append('}');
        }
    }
}
