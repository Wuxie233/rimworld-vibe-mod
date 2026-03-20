using Verse;

namespace VibePlaying
{
    public interface IActionHandler
    {
        string ActionType { get; }
        string Describe(ProposedAction action);
        ActionResult Execute(Map map, ProposedAction action);
    }
}
