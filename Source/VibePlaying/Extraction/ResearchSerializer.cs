using System.Linq;
using System.Text;
using Verse;

namespace VibePlaying
{
    public static class ResearchSerializer
    {
        public static void Append(StringBuilder sb, Map map)
        {
            sb.Append("\"research\":{");

            // Current project
            var current = Find.ResearchManager.GetProject();
            if (current != null)
            {
                float progress = Find.ResearchManager.GetProgress(current);
                sb.Append($"\"current\":{{\"name\":\"{current.defName}\",\"progress\":{progress:F0},\"cost\":{current.baseCost:F0}}},");
            }

            // Completed research
            sb.Append("\"completed\":[");
            var completed = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .Where(r => r.IsFinished)
                .Select(r => r.defName)
                .ToList();

            for (int i = 0; i < completed.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"{completed[i]}\"");
            }
            sb.Append("],");

            // Available (unlocked but not started)
            sb.Append("\"available\":[");
            var available = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .Where(r => !r.IsFinished && r.PrerequisitesCompleted)
                .Select(r => r.defName)
                .Take(10)
                .ToList();

            for (int i = 0; i < available.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"{available[i]}\"");
            }
            sb.Append(']');

            sb.Append('}');
        }
    }
}
