using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VibePlaying
{
    public static class ThreatSerializer
    {
        public static void Append(StringBuilder sb, Map map)
        {
            sb.Append("\"threats\":{");

            // Hostile pawns currently on map
            var hostiles = map.mapPawns.AllPawnsSpawned
                .Where(p => p.HostileTo(Faction.OfPlayer))
                .ToList();

            sb.Append($"\"activeHostiles\":{hostiles.Count},");

            if (hostiles.Count > 0)
            {
                sb.Append("\"hostileGroups\":[");
                var groups = hostiles.GroupBy(p => p.Faction?.Name ?? "Wild");
                bool first = true;
                foreach (var group in groups)
                {
                    if (!first) sb.Append(',');
                    sb.Append($"{{\"faction\":\"{PawnSerializer_EscapeJson(group.Key)}\",\"count\":{group.Count()}}}");
                    first = false;
                }
                sb.Append("],");
            }

            // Defense structures
            int turrets = map.listerBuildings.allBuildingsColonist
                .Count(b => b is Building_TurretGun);
            int traps = map.listerBuildings.allBuildingsColonist
                .Count(b => b is Building_Trap);

            sb.Append($"\"turrets\":{turrets},");
            sb.Append($"\"traps\":{traps},");

            // Colony military strength
            int draftable = map.mapPawns.FreeColonists.Count(p => p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            sb.Append($"\"draftableColonists\":{draftable}");

            sb.Append('}');
        }

        private static string PawnSerializer_EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }
}
