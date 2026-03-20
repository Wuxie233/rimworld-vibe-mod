using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Per-game persistent component. Drives the analysis cycle, stores history,
    /// manages pending actions and their execution.
    /// </summary>
    public class VibePlayingComponent : GameComponent
    {
        private int lastAnalysisTick = -1;
        private bool analysisInProgress;

        private readonly List<AnalysisRecord> history = new List<AnalysisRecord>();
        private readonly List<ProposedAction> pendingActions = new List<ProposedAction>();
        private readonly List<ExecutionLogEntry> executionLog = new List<ExecutionLogEntry>();
        private readonly CommandExecutor executor = new CommandExecutor();

        public IReadOnlyList<AnalysisRecord> History => history;
        public IReadOnlyList<ProposedAction> PendingActions => pendingActions;
        public IReadOnlyList<ExecutionLogEntry> ExecutionLog => executionLog;
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

                    // Parse response for both analysis text and actions
                    var parsed = ResponseParser.Parse(response);

                    history.Insert(0, new AnalysisRecord
                    {
                        Tick = Find.TickManager.TicksGame,
                        Prompt = effectivePrompt,
                        Response = parsed.AnalysisText,
                        ActionCount = parsed.Actions.Count
                    });

                    // Add parsed actions to pending list
                    foreach (var action in parsed.Actions)
                    {
                        if (string.IsNullOrEmpty(action.Description))
                            action.Description = executor.Describe(action);
                        pendingActions.Add(action);
                    }

                    while (history.Count > 20)
                        history.RemoveAt(history.Count - 1);
                });
        }

        public void ApproveAction(int index)
        {
            if (index < 0 || index >= pendingActions.Count) return;
            var action = pendingActions[index];
            action.Status = ActionStatus.Approved;

            var map = Find.CurrentMap;
            if (map == null)
            {
                action.Status = ActionStatus.Failed;
                action.ResultMessage = "No map";
                return;
            }

            var result = executor.Execute(map, action);
            action.Status = result.Success ? ActionStatus.Executed : ActionStatus.Failed;
            action.ResultMessage = result.Message;

            executionLog.Insert(0, new ExecutionLogEntry
            {
                Tick = Find.TickManager.TicksGame,
                ActionType = action.Type,
                Description = action.Description,
                Success = result.Success,
                Message = result.Message
            });

            while (executionLog.Count > 50)
                executionLog.RemoveAt(executionLog.Count - 1);
        }

        public void RejectAction(int index)
        {
            if (index < 0 || index >= pendingActions.Count) return;
            pendingActions[index].Status = ActionStatus.Rejected;
        }

        public void ApproveAll()
        {
            for (int i = 0; i < pendingActions.Count; i++)
            {
                if (pendingActions[i].Status == ActionStatus.Pending)
                    ApproveAction(i);
            }
        }

        public void ClearResolved()
        {
            pendingActions.RemoveAll(a => a.Status != ActionStatus.Pending);
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
        public int ActionCount;
    }

    public class ExecutionLogEntry
    {
        public int Tick;
        public string ActionType;
        public string Description;
        public bool Success;
        public string Message;
    }
}
