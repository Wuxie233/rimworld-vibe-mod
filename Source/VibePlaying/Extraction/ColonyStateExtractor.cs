using System.Text;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Orchestrates all sub-serializers to produce a compact JSON string
    /// representing the current colony state at the requested detail level.
    /// </summary>
    public static class ColonyStateExtractor
    {
        public static string Extract(Map map, DetailLevel level)
        {
            var sb = new StringBuilder(4096);
            sb.Append('{');

            // L0 — always included
            AppendSummary(sb, map);

            if (level >= DetailLevel.L1)
            {
                sb.Append(',');
                PawnSerializer.Append(sb, map, brief: true);
                sb.Append(',');
                ResourceSerializer.Append(sb, map);
                sb.Append(',');
                ThreatSerializer.Append(sb, map);
            }

            if (level >= DetailLevel.L2)
            {
                sb.Append(',');
                PawnSerializer.Append(sb, map, brief: false);
                sb.Append(',');
                ResearchSerializer.Append(sb, map);
                sb.Append(',');
                BuildingSerializer.Append(sb, map);
                sb.Append(',');
                PowerSerializer.Append(sb, map);
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendSummary(StringBuilder sb, Map map)
        {
            var colonists = map.mapPawns.FreeColonistsCount;
            var wealth = map.wealthWatcher.WealthTotal;
            var day = GenDate.DaysPassed;
            var season = GenLocalDate.Season(map);
            var temp = map.mapTemperature.OutdoorTemp;

            sb.Append("\"summary\":{");
            sb.Append($"\"colonists\":{colonists},");
            sb.Append($"\"wealth\":{wealth:F0},");
            sb.Append($"\"day\":{day},");
            sb.Append($"\"season\":\"{season}\",");
            sb.Append($"\"outdoorTemp\":{temp:F1},");

            // Food status
            float foodTotal = 0;
            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
            {
                if (thing.def.IsNutritionGivingIngestible)
                    foodTotal += thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition);
            }
            sb.Append($"\"totalNutrition\":{foodTotal:F1},");
            sb.Append($"\"daysOfFood\":{(colonists > 0 ? foodTotal / (colonists * 1.6f) : 0):F1},");

            // Power
            var powerNets = map.powerNetManager.AllNetsListForReading;
            float totalStored = 0, totalProduction = 0, totalConsumption = 0;
            foreach (var net in powerNets)
            {
                foreach (var comp in net.batteryComps)
                    totalStored += comp.StoredEnergy;
                foreach (var comp in net.powerComps)
                {
                    if (comp.PowerOutput > 0)
                        totalProduction += comp.PowerOutput;
                    else
                        totalConsumption += -comp.PowerOutput;
                }
            }
            sb.Append($"\"power\":{{\"stored\":{totalStored:F0},\"production\":{totalProduction:F0},\"consumption\":{totalConsumption:F0}}}");

            sb.Append('}');
        }
    }
}
