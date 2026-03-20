using System.Linq;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Handles designations: hunt, mine, harvest, cut trees.
    /// </summary>
    public class DesignateHandler : IActionHandler
    {
        public string ActionType => "designate";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("action", out var designAction);
            action.Params.TryGetValue("target", out var target);
            return $"Designate {designAction}: {target}";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("action", out var designAction))
                return ActionResult.Fail("Missing 'action' parameter");

            switch (designAction.ToLower())
            {
                case "hunt":
                    return ExecuteHunt(map, action);
                case "mine":
                    return ExecuteMine(map, action);
                case "cut":
                    return ExecuteCut(map, action);
                case "harvest":
                    return ExecuteHarvest(map, action);
                default:
                    return ActionResult.Fail($"Unknown designation action: {designAction}");
            }
        }

        private ActionResult ExecuteHunt(Map map, ProposedAction action)
        {
            action.Params.TryGetValue("target", out var targetDef);
            int maxCount = 5;
            if (action.Params.TryGetValue("count", out var countStr) && int.TryParse(countStr, out int c))
                maxCount = c;

            var animals = map.mapPawns.AllPawnsSpawned
                .Where(p => p.RaceProps.Animal && !p.HostileTo(Faction.OfPlayer) && p.Faction == null)
                .Where(p => string.IsNullOrEmpty(targetDef) || p.def.defName.ToLower().Contains(targetDef.ToLower()))
                .Take(maxCount)
                .ToList();

            if (animals.Count == 0)
                return ActionResult.Fail($"No huntable animals found matching '{targetDef}'");

            int designated = 0;
            var designator = new Designator_Hunt();
            foreach (var animal in animals)
            {
                if (map.designationManager.DesignationOn(animal, DesignationDefOf.Hunt) != null)
                    continue;
                map.designationManager.AddDesignation(new Designation(animal, DesignationDefOf.Hunt));
                designated++;
            }

            return ActionResult.Ok($"Designated {designated} animals for hunting");
        }

        private ActionResult ExecuteMine(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out int x))
                return ActionResult.Fail("Missing x for mine");
            if (!action.Params.TryGetValue("z", out var zStr) || !int.TryParse(zStr, out int z))
                return ActionResult.Fail("Missing z for mine");

            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map))
                return ActionResult.Fail($"({x},{z}) out of bounds");

            var mineable = cell.GetFirstMineable(map);
            if (mineable == null)
                return ActionResult.Fail($"No mineable rock at ({x},{z})");

            if (map.designationManager.DesignationAt(cell, DesignationDefOf.Mine) != null)
                return ActionResult.Fail($"Already designated for mining at ({x},{z})");

            map.designationManager.AddDesignation(new Designation(cell, DesignationDefOf.Mine));
            return ActionResult.Ok($"Designated mining at ({x},{z})");
        }

        private ActionResult ExecuteCut(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out int x))
                return ActionResult.Fail("Missing x for cut");
            if (!action.Params.TryGetValue("z", out var zStr) || !int.TryParse(zStr, out int z))
                return ActionResult.Fail("Missing z for cut");

            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map))
                return ActionResult.Fail($"({x},{z}) out of bounds");

            var plant = cell.GetPlant(map);
            if (plant == null)
                return ActionResult.Fail($"No plant at ({x},{z})");

            if (map.designationManager.DesignationOn(plant, DesignationDefOf.CutPlant) != null)
                return ActionResult.Fail($"Already designated for cutting");

            map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
            return ActionResult.Ok($"Designated cut plant at ({x},{z})");
        }

        private ActionResult ExecuteHarvest(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("x", out var xStr) || !int.TryParse(xStr, out int x))
                return ActionResult.Fail("Missing x for harvest");
            if (!action.Params.TryGetValue("z", out var zStr) || !int.TryParse(zStr, out int z))
                return ActionResult.Fail("Missing z for harvest");

            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map))
                return ActionResult.Fail($"({x},{z}) out of bounds");

            var plant = cell.GetPlant(map);
            if (plant == null || !plant.HarvestableNow)
                return ActionResult.Fail($"No harvestable plant at ({x},{z})");

            if (map.designationManager.DesignationOn(plant, DesignationDefOf.HarvestPlant) != null)
                return ActionResult.Fail($"Already designated for harvest");

            map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.HarvestPlant));
            return ActionResult.Ok($"Designated harvest at ({x},{z})");
        }
    }
}
