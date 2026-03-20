using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace VibePlaying
{
    public static class PawnSerializer
    {
        public static void Append(StringBuilder sb, Map map, bool brief)
        {
            var colonists = map.mapPawns.FreeColonists.ToList();
            var key = brief ? "\"pawnSummary\"" : "\"pawnDetails\"";
            sb.Append($"{key}:[");

            for (int i = 0; i < colonists.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var pawn = colonists[i];

                sb.Append('{');
                sb.Append($"\"name\":\"{EscapeJson(pawn.LabelShort)}\",");
                sb.Append($"\"age\":{pawn.ageTracker.AgeBiologicalYears},");

                // Mood
                if (pawn.needs?.mood != null)
                    sb.Append($"\"mood\":{pawn.needs.mood.CurLevelPercentage:F2},");

                // Health
                sb.Append($"\"health\":{pawn.health.summaryHealth.SummaryHealthPercent:F2},");

                if (brief)
                {
                    // Top 3 skills
                    sb.Append("\"topSkills\":[");
                    var skills = pawn.skills.skills
                        .Where(s => !s.TotallyDisabled)
                        .OrderByDescending(s => s.Level)
                        .Take(3);
                    bool first = true;
                    foreach (var skill in skills)
                    {
                        if (!first) sb.Append(',');
                        sb.Append($"{{\"skill\":\"{skill.def.defName}\",\"level\":{skill.Level}}}");
                        first = false;
                    }
                    sb.Append("],");

                    // Current activity
                    var curJob = pawn.CurJobDef;
                    sb.Append($"\"activity\":\"{EscapeJson(curJob?.defName ?? "Idle")}\"");
                }
                else
                {
                    // Full skills
                    sb.Append("\"skills\":[");
                    bool first = true;
                    foreach (var skill in pawn.skills.skills)
                    {
                        if (!first) sb.Append(',');
                        sb.Append($"{{\"skill\":\"{skill.def.defName}\",\"level\":{skill.Level},\"passion\":\"{skill.passion}\",\"disabled\":{(skill.TotallyDisabled ? "true" : "false")}}}");
                        first = false;
                    }
                    sb.Append("],");

                    // Traits
                    sb.Append("\"traits\":[");
                    first = true;
                    foreach (var trait in pawn.story.traits.allTraits)
                    {
                        if (!first) sb.Append(',');
                        sb.Append($"\"{EscapeJson(trait.CurrentData.label)}\"");
                        first = false;
                    }
                    sb.Append("],");

                    // Hediffs (health conditions)
                    sb.Append("\"hediffs\":[");
                    first = true;
                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (!first) sb.Append(',');
                        sb.Append($"{{\"label\":\"{EscapeJson(hediff.Label)}\",\"severity\":{hediff.Severity:F2}}}");
                        first = false;
                    }
                    sb.Append("],");

                    // Work priorities
                    sb.Append("\"workPriorities\":{");
                    first = true;
                    foreach (var workDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        if (pawn.WorkTypeIsDisabled(workDef)) continue;
                        if (!first) sb.Append(',');
                        sb.Append($"\"{workDef.defName}\":{pawn.workSettings.GetPriority(workDef)}");
                        first = false;
                    }
                    sb.Append('}');
                }

                sb.Append('}');
            }
            sb.Append(']');
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }
}
