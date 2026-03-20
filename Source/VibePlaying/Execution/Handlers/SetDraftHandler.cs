using System.Linq;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Drafts or undrafts a colonist for combat.
    /// </summary>
    public class SetDraftHandler : IActionHandler
    {
        public string ActionType => "set_draft";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("pawn_name", out var pawn);
            action.Params.TryGetValue("drafted", out var drafted);
            var state = drafted?.ToLower() == "true" ? "Draft" : "Undraft";
            return $"{state} {pawn}";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!action.Params.TryGetValue("pawn_name", out var pawnName))
                return ActionResult.Fail("Missing pawn_name");
            if (!action.Params.TryGetValue("drafted", out var draftedStr))
                return ActionResult.Fail("Missing drafted parameter");

            bool shouldDraft = draftedStr.ToLower() == "true";

            var pawn = map.mapPawns.FreeColonists
                .FirstOrDefault(p => p.LabelShort.ToLower() == pawnName.ToLower());
            if (pawn == null)
                return ActionResult.Fail($"Pawn '{pawnName}' not found");

            if (pawn.Downed)
                return ActionResult.Fail($"{pawnName} is downed and cannot be drafted");

            if (pawn.InMentalState)
                return ActionResult.Fail($"{pawnName} is in mental break");

            pawn.drafter.Drafted = shouldDraft;
            var verb = shouldDraft ? "Drafted" : "Undrafted";
            return ActionResult.Ok($"{verb} {pawnName}");
        }
    }
}
