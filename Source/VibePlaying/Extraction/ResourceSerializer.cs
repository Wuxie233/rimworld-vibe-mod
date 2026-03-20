using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace VibePlaying
{
    public static class ResourceSerializer
    {
        // Key resources to always report
        private static readonly string[] TrackedResources = new[]
        {
            "Steel", "WoodLog", "Plasteel", "Uranium", "Gold", "Silver", "Jade",
            "ComponentIndustrial", "ComponentSpacer",
            "Cloth", "DevilstrandCloth", "Hyperweave",
            "MedicineHerbal", "MedicineIndustrial", "MedicineUltratech",
            "Chemfuel", "Neutroamine"
        };

        public static void Append(StringBuilder sb, Map map)
        {
            sb.Append("\"resources\":{");

            // Tracked resources with explicit counts
            var counts = new Dictionary<string, int>();
            foreach (var defName in TrackedResources)
                counts[defName] = 0;

            foreach (var zone in map.zoneManager.AllZones)
            {
                if (!(zone is Zone_Stockpile)) continue;
                foreach (var thing in zone.AllContainedThings)
                {
                    if (thing.def == null) continue;
                    if (counts.ContainsKey(thing.def.defName))
                        counts[thing.def.defName] += thing.stackCount;
                }
            }

            // Also check buildings with inner containers (shelves, etc.)
            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways))
            {
                if (thing.def == null) continue;
                if (counts.ContainsKey(thing.def.defName))
                    counts[thing.def.defName] += thing.stackCount;
            }

            // De-duplicate: just use map resource count API
            sb.Append("\"stockpile\":{");
            bool first = true;
            foreach (var defName in TrackedResources)
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null) continue;
                int count = map.resourceCounter.GetCount(def);
                if (count == 0) continue;

                if (!first) sb.Append(',');
                sb.Append($"\"{defName}\":{count}");
                first = false;
            }
            sb.Append("},");

            // Meal counts by type
            sb.Append("\"meals\":{");
            first = true;
            foreach (var thing in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
            {
                if (!thing.def.IsNutritionGivingIngestible) continue;
                if (thing.def.ingestible?.preferability < FoodPreferability.MealAwful) continue;

                if (!first) sb.Append(',');
                sb.Append($"\"{thing.def.defName}\":{thing.stackCount}");
                first = false;
            }
            sb.Append('}');

            sb.Append('}');
        }
    }
}
