using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Places a predefined building template at specified origin coordinates.
    /// The AI picks a template name + origin + optional stuff material.
    /// </summary>
    public class PlaceTemplateHandler : IActionHandler
    {
        public string ActionType => "place_template";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("template", out var name);
            action.Params.TryGetValue("x", out var x);
            action.Params.TryGetValue("z", out var z);
            return $"Place template '{name}' at ({x},{z})";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("template", out var templateName))
                return ActionResult.Fail("Missing template name");
            if (!action.Params.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out int originX))
                return ActionResult.Fail("Missing or invalid x");
            if (!action.Params.TryGetValue("z", out var zStr) || !int.TryParse(zStr, out int originZ))
                return ActionResult.Fail("Missing or invalid z");

            var template = TemplateLibrary.Get(templateName);
            if (template == null)
                return ActionResult.Fail($"Unknown template: '{templateName}'. Available: {string.Join(", ", TemplateLibrary.Names)}");

            // Resolve stuff material
            ThingDef stuffDef = null;
            if (action.Params.TryGetValue("stuff", out var stuffName) && !string.IsNullOrEmpty(stuffName))
            {
                stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(stuffName);
            }

            int placed = 0, skipped = 0;
            foreach (var entry in template.Entries)
            {
                var cell = new IntVec3(originX + entry.DX, 0, originZ + entry.DZ);
                if (!cell.InBounds(map)) { skipped++; continue; }

                var buildingDef = DefDatabase<ThingDef>.GetNamedSilentFail(entry.BuildingDef);
                if (buildingDef == null) { skipped++; continue; }

                // Determine stuff: entry override > action parameter > first available
                ThingDef entryStuff = null;
                if (!string.IsNullOrEmpty(entry.StuffDef))
                    entryStuff = DefDatabase<ThingDef>.GetNamedSilentFail(entry.StuffDef);

                var finalStuff = entryStuff ?? stuffDef;
                if (finalStuff == null && buildingDef.MadeFromStuff)
                    finalStuff = GenStuff.DefaultStuffFor(buildingDef);

                var rot = new Rot4(entry.Rotation);

                // Skip if something already built/blueprinted here
                if (cell.GetFirstBuilding(map) != null) { skipped++; continue; }

                GenConstruct.PlaceBlueprintForBuild(buildingDef, cell, map, rot, Faction.OfPlayer, finalStuff);
                placed++;
            }

            return ActionResult.Ok($"Template '{templateName}': placed {placed} blueprints, skipped {skipped}");
        }
    }
}
