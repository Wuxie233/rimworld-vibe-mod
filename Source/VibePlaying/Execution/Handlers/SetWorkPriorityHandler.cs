using System.Linq;
using RimWorld;
using Verse;

namespace VibePlaying
{
    public class SetWorkPriorityHandler : IActionHandler
    {
        public string ActionType => "set_work_priority";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("pawn_name", out var pawn);
            action.Params.TryGetValue("work_type", out var work);
            action.Params.TryGetValue("priority", out var priority);
            return $"Set {pawn}'s {work} priority to {priority}";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("pawn_name", out var pawnName))
                return ActionResult.Fail("Missing pawn_name");
            if (!action.Params.TryGetValue("work_type", out var workType))
                return ActionResult.Fail("Missing work_type");
            if (!action.Params.TryGetValue("priority", out var priorityStr) || !int.TryParse(priorityStr, out int priority))
                return ActionResult.Fail("Missing or invalid priority");

            if (priority < 0 || priority > 4)
                return ActionResult.Fail($"Priority must be 0-4, got {priority}");

            var pawn = map.mapPawns.FreeColonists
                .FirstOrDefault(p => p.LabelShort.ToLower() == pawnName.ToLower());
            if (pawn == null)
                return ActionResult.Fail($"Pawn '{pawnName}' not found");

            var workDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workType);
            if (workDef == null)
                return ActionResult.Fail($"Work type '{workType}' not found");

            if (pawn.WorkTypeIsDisabled(workDef))
                return ActionResult.Fail($"{pawnName} cannot do {workType} (incapable)");

            pawn.workSettings.SetPriority(workDef, priority);
            return ActionResult.Ok($"Set {pawnName}'s {workDef.labelShort} to priority {priority}");
        }
    }
}
