using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Dispatches ProposedActions to the appropriate IActionHandler.
    /// </summary>
    public class CommandExecutor
    {
        private readonly Dictionary<string, IActionHandler> handlers = new Dictionary<string, IActionHandler>();

        public CommandExecutor()
        {
            Register(new SetWorkPriorityHandler());
            Register(new PlaceBlueprintHandler());
            Register(new DesignateHandler());
            Register(new QueueBillHandler());
            Register(new SendReportHandler());
            Register(new CreateZoneHandler());
            Register(new SetDraftHandler());
        }

        private void Register(IActionHandler handler)
        {
            handlers[handler.ActionType] = handler;
        }

        public string Describe(ProposedAction action)
        {
            if (handlers.TryGetValue(action.Type, out var handler))
                return handler.Describe(action);
            return $"Unknown action: {action.Type}";
        }

        public ActionResult Execute(Map map, ProposedAction action)
        {
            if (!handlers.TryGetValue(action.Type, out var handler))
                return ActionResult.Fail($"No handler for action type '{action.Type}'");

            return handler.Execute(map, action);
        }

        public IEnumerable<string> SupportedTypes => handlers.Keys;
    }
}
