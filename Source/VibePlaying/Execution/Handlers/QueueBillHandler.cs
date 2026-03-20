using System.Linq;
using RimWorld;
using Verse;

namespace VibePlaying
{
    public class QueueBillHandler : IActionHandler
    {
        public string ActionType => "queue_bill";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("workbench_def", out var bench);
            action.Params.TryGetValue("recipe_def", out var recipe);
            action.Params.TryGetValue("count", out var count);
            return $"Queue {count}x {recipe} at {bench}";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("recipe_def", out var recipeName))
                return ActionResult.Fail("Missing recipe_def");

            int count = 1;
            if (action.Params.TryGetValue("count", out var countStr) && int.TryParse(countStr, out int c))
                count = c;

            var recipeDef = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
            if (recipeDef == null)
                return ActionResult.Fail($"Recipe '{recipeName}' not found");

            // Find a workbench that can make this recipe
            Building_WorkTable workbench = null;

            if (action.Params.TryGetValue("workbench_def", out var benchName) && !string.IsNullOrEmpty(benchName))
            {
                workbench = map.listerBuildings.allBuildingsColonist
                    .OfType<Building_WorkTable>()
                    .FirstOrDefault(b => b.def.defName == benchName && recipeDef.AvailableOnNow(b));
            }

            if (workbench == null)
            {
                workbench = map.listerBuildings.allBuildingsColonist
                    .OfType<Building_WorkTable>()
                    .FirstOrDefault(b => recipeDef.AvailableOnNow(b));
            }

            if (workbench == null)
                return ActionResult.Fail($"No available workbench for recipe '{recipeName}'");

            var bill = recipeDef.MakeNewBill();
            if (bill is Bill_Production prodBill)
            {
                prodBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
                prodBill.repeatCount = count;
            }
            workbench.BillStack.AddBill(bill);

            return ActionResult.Ok($"Queued {count}x {recipeDef.label} at {workbench.LabelShort}");
        }
    }
}
