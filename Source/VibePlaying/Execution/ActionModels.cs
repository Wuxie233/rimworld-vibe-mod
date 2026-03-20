using System.Collections.Generic;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Represents a single action proposed by the LLM.
    /// </summary>
    public class ProposedAction : IExposable
    {
        public string Type;
        public Dictionary<string, string> Params = new Dictionary<string, string>();
        public string Description;
        public ActionStatus Status = ActionStatus.Pending;
        public string ResultMessage;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Type, "type");
            Scribe_Values.Look(ref Description, "description");
            Scribe_Values.Look(ref Status, "status");
            Scribe_Values.Look(ref ResultMessage, "resultMessage");
            // Params are transient — only live during the session
        }
    }

    public enum ActionStatus
    {
        Pending,
        Approved,
        Rejected,
        Executed,
        Failed
    }

    /// <summary>
    /// Result of executing an action.
    /// </summary>
    public class ActionResult
    {
        public bool Success;
        public string Message;

        public static ActionResult Ok(string msg = "OK") => new ActionResult { Success = true, Message = msg };
        public static ActionResult Fail(string msg) => new ActionResult { Success = false, Message = msg };
    }

    /// <summary>
    /// Response from LLM that contains both analysis text and proposed actions.
    /// </summary>
    public class LLMAnalysisResponse
    {
        public string AnalysisText;
        public List<ProposedAction> Actions = new List<ProposedAction>();
    }
}
