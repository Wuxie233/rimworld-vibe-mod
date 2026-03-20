using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Handles "send_report" action — AI sends analysis reports to the player.
    /// These are display-only and don't modify game state.
    /// </summary>
    public class SendReportHandler : IActionHandler
    {
        public string ActionType => "send_report";

        public string Describe(ProposedAction action)
        {
            action.Params.TryGetValue("title", out var title);
            return $"Report: {title}";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            action.Params.TryGetValue("title", out var title);
            action.Params.TryGetValue("content", out var content);
            action.Params.TryGetValue("severity", out var severity);

            var messageType = MessageTypeDefOf.NeutralEvent;
            if (severity == "warning")
                messageType = MessageTypeDefOf.CautionInput;
            else if (severity == "critical")
                messageType = MessageTypeDefOf.ThreatBig;

            Messages.Message($"[VibePlaying] {title}: {content}", messageType, false);
            return ActionResult.Ok($"Report sent: {title}");
        }
    }
}
