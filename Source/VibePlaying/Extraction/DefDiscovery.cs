using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Scans the DefDatabase at runtime to produce a compact inventory of
    /// available defNames. This is appended to the colony state JSON so the
    /// LLM knows which exact identifiers to use (including third-party mods).
    /// </summary>
    public static class DefDiscovery
    {
        /// <summary>
        /// Appends a "availableDefs" section to the colony state JSON.
        /// Called once per analysis cycle (not every tick).
        /// </summary>
        public static void Append(StringBuilder sb, Map map)
        {
            sb.Append("\"availableDefs\":{");

            // Work types
            sb.Append("\"workTypes\":[");
            sb.Append(string.Join(",", DefDatabase<WorkTypeDef>.AllDefsListForReading
                .OrderBy(d => d.naturalPriority)
                .Select(d => $"\"{d.defName}\"")));
            sb.Append("],");

            // Buildable things (player-buildable, top 50 by label)
            sb.Append("\"buildable\":[");
            var buildable = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.BuildableByPlayer)
                .OrderBy(d => d.label)
                .Take(50)
                .Select(d => $"\"{d.defName}\"");
            sb.Append(string.Join(",", buildable));
            sb.Append("],");

            // Recipes available on placed workbenches
            sb.Append("\"recipes\":[");
            var workbenches = map.listerBuildings.allBuildingsColonist
                .Select(b => b.def)
                .Where(d => d.recipes != null)
                .Distinct();

            var recipes = new HashSet<string>();
            foreach (var bench in workbenches)
            {
                foreach (var recipe in bench.recipes)
                    recipes.Add(recipe.defName);
            }
            // Also include recipes from AllRecipes (includes those added by implants etc)
            sb.Append(string.Join(",", recipes.OrderBy(r => r).Take(60).Select(r => $"\"{r}\"")));
            sb.Append("],");

            // Plants that can be grown
            sb.Append("\"growable\":[");
            var plants = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.plant != null && d.plant.sowTags != null && d.plant.sowTags.Count > 0)
                .OrderBy(d => d.label)
                .Select(d => $"\"{d.defName}\"");
            sb.Append(string.Join(",", plants));
            sb.Append("],");

            // Stuff materials (wood, stone, metal)
            sb.Append("\"stuff\":[");
            var stuff = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.IsStuff)
                .OrderBy(d => d.label)
                .Take(30)
                .Select(d => $"\"{d.defName}\"");
            sb.Append(string.Join(",", stuff));
            sb.Append("]");

            sb.Append('}');
        }
    }
}
