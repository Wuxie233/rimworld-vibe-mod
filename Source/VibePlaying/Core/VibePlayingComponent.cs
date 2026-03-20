using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Per-game persistent component. Drives the analysis cycle and stores history.
    /// </summary>
    public class VibePlayingComponent : GameComponent
    {
        private int lastAnalysisTick = -1;
        private bool analysisInProgress;

        // Analysis history kept in memory for UI display (not serialized beyond last few)
        private readonly List<AnalysisRecord> history = new List<AnalysisRecord>();

        public IReadOnlyList<AnalysisRecord> History => history;
        public bool IsAnalyzing => analysisInProgress;
        public string CurrentStrategy = "";

        public VibePlayingComponent(Game game) { }

        public override void GameComponentTick()
        {
            if (!VibePlayingMod.Settings.autoAnalyze) return;
            if (analysisInProgress) return;

            int intervalTicks = VibePlayingMod.Settings.analysisCycleDays * 60000;
            int currentTick = Find.TickManager.TicksGame;

            if (lastAnalysisTick < 0 || currentTick - lastAnalysisTick >= intervalTicks)
            {
                TriggerAnalysis(null);
            }
        }

        public void TriggerAnalysis(string userPrompt)
        {
            if (analysisInProgress) return;
            analysisInProgress = true;
            lastAnalysisTick = Find.TickManager.TicksGame;

            var map = Find.CurrentMap;
            if (map == null)
            {
                analysisInProgress = false;
                return;
            }

            var stateJson = ColonyStateExtractor.Extract(map, DetailLevel.L1);
            var effectivePrompt = string.IsNullOrEmpty(userPrompt) ? CurrentStrategy : userPrompt;

            LLMClient.AnalyzeAsync(stateJson, effectivePrompt, VibePlayingMod.Settings,
                (response, error) =>
                {
                    analysisInProgress = false;
                    if (error != null)
                    {
                        Log.Warning($"[VibePlaying] Analysis failed: {error}");
                        history.Insert(0, new AnalysisRecord
                        {
                            Tick = Find.TickManager.TicksGame,
                            Prompt = effectivePrompt,
                            Response = $"Error: {error}",
                            IsError = true
                        });
                        return;
                    }

                    history.Insert(0, new AnalysisRecord
                    {
                        Tick = Find.TickManager.TicksGame,
                        Prompt = effectivePrompt,
                        Response = response
                    });

                    // Cap history at 20 entries
                    while (history.Count > 20)
                        history.RemoveAt(history.Count - 1);
                });
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastAnalysisTick, "lastAnalysisTick", -1);
            Scribe_Values.Look(ref CurrentStrategy, "currentStrategy", "");
        }
    }

    public class AnalysisRecord
    {
        public int Tick;
        public string Prompt;
        public string Response;
        public bool IsError;
    }
}
