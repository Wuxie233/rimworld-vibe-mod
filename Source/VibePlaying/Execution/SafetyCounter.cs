using System.Collections.Generic;

namespace VibePlaying
{
    /// <summary>
    /// Tracks action counts per cycle to enforce safety limits.
    /// Resets when a new analysis cycle starts.
    /// </summary>
    public class SafetyCounter
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private int lastResetTick = -1;

        public void ResetIfNewCycle(int currentTick, int cycleTicks)
        {
            if (lastResetTick < 0 || currentTick - lastResetTick >= cycleTicks)
            {
                counts.Clear();
                lastResetTick = currentTick;
            }
        }

        public int GetCount(string actionType)
        {
            return counts.TryGetValue(actionType, out int c) ? c : 0;
        }

        public void Increment(string actionType)
        {
            if (counts.ContainsKey(actionType))
                counts[actionType]++;
            else
                counts[actionType] = 1;
        }

        public bool IsWithinLimit(string actionType, VibePlayingSettings settings)
        {
            int current = GetCount(actionType);
            switch (actionType)
            {
                case "place_blueprint": return current < settings.maxBlueprintsPerCycle;
                case "designate": return current < settings.maxDesignationsPerCycle;
                case "queue_bill": return current < settings.maxBillsPerCycle;
                case "set_work_priority": return current < settings.maxWorkChangesPerCycle;
                case "send_report": return true; // No limit on reports
                default: return true;
            }
        }
    }
}
