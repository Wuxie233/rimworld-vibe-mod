using System.Linq;
using RimWorld;
using Verse;

namespace VibePlaying
{
    public class PlaceBlueprintHandler : IActionHandler
    {
        public string ActionType => "place_blueprint";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("building_def", out var def);
            action.Params.TryGetValue("x", out var x);
            action.Params.TryGetValue("z", out var z);
            action.Params.TryGetValue("stuff", out var stuff);
            var desc = $"Place {def} at ({x},{z})";
            if (!string.IsNullOrEmpty(stuff)) desc += $" using {stuff}";
            return desc;
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("building_def", out var buildingDefName))
                return ActionResult.Fail("Missing building_def");
            if (!action.Params.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out int x))
                return ActionResult.Fail("Missing or invalid x");
            if (!action.Params.TryGetValue("z", out var zStr) || !int.TryParse(zStr, out int z))
                return ActionResult.Fail("Missing or invalid z");

            var thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(buildingDefName);
            if (thingDef == null)
                return ActionResult.Fail($"Building def '{buildingDefName}' not found");

            if (!thingDef.IsBlueprint && thingDef.blueprintDef == null)
                return ActionResult.Fail($"'{buildingDefName}' is not a buildable thing");

            // Rotation
            var rot = Rot4.North;
            if (action.Params.TryGetValue("rotation", out var rotStr) && int.TryParse(rotStr, out int rotInt))
                rot = new Rot4(rotInt);

            // Stuff material
            ThingDef stuff = null;
            if (action.Params.TryGetValue("stuff", out var stuffName) && !string.IsNullOrEmpty(stuffName))
            {
                stuff = DefDatabase<ThingDef>.GetNamedSilentFail(stuffName);
                if (stuff == null)
                    return ActionResult.Fail($"Stuff '{stuffName}' not found");
            }
            else if (thingDef.MadeFromStuff)
            {
                stuff = GenStuff.DefaultStuffFor(thingDef);
            }

            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map))
                return ActionResult.Fail($"Position ({x},{z}) is out of map bounds");

            // Check if can place
            if (!GenConstruct.CanPlaceBlueprintAt(thingDef, cell, rot, map).Accepted)
                return ActionResult.Fail($"Cannot place {buildingDefName} at ({x},{z}) — blocked or invalid");

            GenConstruct.PlaceBlueprintForBuild(thingDef, cell, map, rot, Faction.OfPlayer, stuff);
            return ActionResult.Ok($"Placed {buildingDefName} blueprint at ({x},{z})");
        }
    }
}
