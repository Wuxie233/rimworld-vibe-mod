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

        // Safety limits
        public int maxBlueprintsPerCycle = 30;
        public int maxDesignationsPerCycle = 50;

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
            Scribe_Values.Look(ref maxBlueprintsPerCycle, "maxBlueprintsPerCycle", 30);
            Scribe_Values.Look(ref maxDesignationsPerCycle, "maxDesignationsPerCycle", 50);
        }
    }
}
