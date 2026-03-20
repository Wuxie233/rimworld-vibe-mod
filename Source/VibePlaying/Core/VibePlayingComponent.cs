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
        private readonly SafetyCounter safetyCounter = new SafetyCounter();
        private List<ExecutionLogEntry> executionLogSave;
        private List<AnalysisRecord> historySave;

        public IReadOnlyList<AnalysisRecord> History => history;
        public IReadOnlyList<ProposedAction> PendingActions => pendingActions;
        public IReadOnlyList<ExecutionLogEntry> ExecutionLog => executionLog;
        public SafetyCounter Safety => safetyCounter;
        public bool IsAnalyzing => analysisInProgress;
        public string CurrentStrategy = "";
        private string pendingEventPrompt;
        private int eventAnalysisTick = -1;
        private const int EventDelayTicks = 300; // ~5 seconds at normal speed

        public VibePlayingComponent(Game game) { }

        /// <summary>
        /// Queue an event-triggered analysis with a short delay.
        /// This avoids analyzing mid-spawn (e.g. raid pawns still arriving).
        /// </summary>
        public void QueueEventAnalysis(string prompt)
        {
            if (analysisInProgress) return;
            pendingEventPrompt = prompt;
            eventAnalysisTick = Find.TickManager.TicksGame + EventDelayTicks;
        }

        public override void GameComponentTick()
        {
            if (analysisInProgress) return;

            int currentTick = Find.TickManager.TicksGame;

            // Fire queued event analysis after delay
            if (pendingEventPrompt != null && currentTick >= eventAnalysisTick)
            {
                var prompt = pendingEventPrompt;
                pendingEventPrompt = null;
                eventAnalysisTick = -1;
                TriggerAnalysis(prompt);
                return;
            }

            if (!VibePlayingMod.Settings.autoAnalyze) return;

            int intervalTicks = VibePlayingMod.Settings.analysisCycleDays * 60000;

            // Reset safety counters at cycle boundary
            safetyCounter.ResetIfNewCycle(currentTick, intervalTicks);

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

            LLMClient.AnalyzeAsync(stateJson, effectivePrompt, VibePlayingMod.Settings, history,
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

                    // Add parsed actions — auto-execute if configured
                    var settings = VibePlayingMod.Settings;
                    var currentMap = Find.CurrentMap;
                    foreach (var action in parsed.Actions)
                    {
                        if (string.IsNullOrEmpty(action.Description))
                            action.Description = executor.Describe(action);

                        if (settings.IsAutoExec(action.Type)
                            && safetyCounter.IsWithinLimit(action.Type, settings)
                            && currentMap != null)
                        {
                            var result = executor.Execute(currentMap, action);
                            action.Status = result.Success ? ActionStatus.Executed : ActionStatus.Failed;
                            action.ResultMessage = result.Message;
                            if (result.Success)
                                safetyCounter.Increment(action.Type);

                            executionLog.Insert(0, new ExecutionLogEntry
                            {
                                Tick = Find.TickManager.TicksGame,
                                ActionType = action.Type,
                                Description = "[AUTO] " + action.Description,
                                Success = result.Success,
                                Message = result.Message
                            });
                        }

                        pendingActions.Add(action);
                    }

                    while (history.Count > 20)
                        history.RemoveAt(history.Count - 1);
                    while (executionLog.Count > 50)
                        executionLog.RemoveAt(executionLog.Count - 1);
                });
        }

        public void ApproveAction(int index)
        {
            if (index < 0 || index >= pendingActions.Count) return;
            var action = pendingActions[index];

            // Safety limit check
            if (!safetyCounter.IsWithinLimit(action.Type, VibePlayingMod.Settings))
            {
                action.Status = ActionStatus.Failed;
                action.ResultMessage = "Safety limit reached for this cycle";
                return;
            }

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
            if (result.Success)
                safetyCounter.Increment(action.Type);

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
            Scribe_Collections.Look(ref executionLogSave, "executionLog", LookMode.Deep);
            Scribe_Collections.Look(ref historySave, "history", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (executionLogSave != null)
                {
                    executionLog.Clear();
                    executionLog.AddRange(executionLogSave);
                }
                if (historySave != null)
                {
                    history.Clear();
                    history.AddRange(historySave);
                }
            }
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                executionLogSave = new List<ExecutionLogEntry>(executionLog);
                historySave = new List<AnalysisRecord>(history);
            }
        }
    }

    public class AnalysisRecord : IExposable
    {
        public int Tick;
        public string Prompt;
        public string Response;
        public bool IsError;
        public int ActionCount;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref Prompt, "prompt");
            Scribe_Values.Look(ref Response, "response");
            Scribe_Values.Look(ref IsError, "isError");
            Scribe_Values.Look(ref ActionCount, "actionCount");
        }
    }

    public class ExecutionLogEntry : IExposable
    {
        public int Tick;
        public string ActionType;
        public string Description;
        public bool Success;
        public string Message;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Tick, "tick");
            Scribe_Values.Look(ref ActionType, "actionType");
            Scribe_Values.Look(ref Description, "description");
            Scribe_Values.Look(ref Success, "success");
            Scribe_Values.Look(ref Message, "message");
        }
    }
}
