using Verse;

namespace VibePlaying
{
    public class VibePlayingSettings : ModSettings
    {
        // LLM API configuration
        public string apiEndpoint = "http://localhost:11434/v1/chat/completions";
        public string apiKey = "";
        public string model = "qwen2.5:14b";
        public int maxTokens = 4096;
        public float temperature = 0.3f;
        public int timeoutSeconds = 30;

        // Analysis behavior
        public bool autoAnalyze;
        public int analysisCycleDays = 1;

        // Auto-execution: per action type
        public bool autoExecWorkPriority;
        public bool autoExecBlueprint;
        public bool autoExecDesignate;
        public bool autoExecBill;
        public bool autoExecReport = true; // Reports are safe, auto-execute by default

        // Safety limits per cycle
        public int maxBlueprintsPerCycle = 30;
        public int maxDesignationsPerCycle = 50;
        public int maxBillsPerCycle = 20;
        public int maxWorkChangesPerCycle = 20;

        public bool IsAutoExec(string actionType)
        {
            switch (actionType)
            {
                case "set_work_priority": return autoExecWorkPriority;
                case "place_blueprint": return autoExecBlueprint;
                case "designate": return autoExecDesignate;
                case "queue_bill": return autoExecBill;
                case "send_report": return autoExecReport;
                default: return false;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref apiEndpoint, "apiEndpoint", "http://localhost:11434/v1/chat/completions");
            Scribe_Values.Look(ref apiKey, "apiKey", "");
            Scribe_Values.Look(ref model, "model", "qwen2.5:14b");
            Scribe_Values.Look(ref maxTokens, "maxTokens", 4096);
            Scribe_Values.Look(ref temperature, "temperature", 0.3f);
            Scribe_Values.Look(ref timeoutSeconds, "timeoutSeconds", 30);
            Scribe_Values.Look(ref autoAnalyze, "autoAnalyze", false);
            Scribe_Values.Look(ref analysisCycleDays, "analysisCycleDays", 1);
            Scribe_Values.Look(ref autoExecWorkPriority, "autoExecWorkPriority", false);
            Scribe_Values.Look(ref autoExecBlueprint, "autoExecBlueprint", false);
            Scribe_Values.Look(ref autoExecDesignate, "autoExecDesignate", false);
            Scribe_Values.Look(ref autoExecBill, "autoExecBill", false);
            Scribe_Values.Look(ref autoExecReport, "autoExecReport", true);
            Scribe_Values.Look(ref maxBlueprintsPerCycle, "maxBlueprintsPerCycle", 30);
            Scribe_Values.Look(ref maxDesignationsPerCycle, "maxDesignationsPerCycle", 50);
            Scribe_Values.Look(ref maxBillsPerCycle, "maxBillsPerCycle", 20);
            Scribe_Values.Look(ref maxWorkChangesPerCycle, "maxWorkChangesPerCycle", 20);
        }
    }
}
