using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VibePlaying
{
    public class MainButtonWorker_VibePlaying : MainButtonWorker
    {
        public override void Activate()
        {
            Find.WindowStack.Add(new VibePlayingWindow());
        }
    }

    public class VibePlayingWindow : Window
    {
        private string inputText = "";
        private Vector2 scrollPos;
        private int activeTab; // 0=Analysis, 1=Pending Actions, 2=Execution Log

        public override Vector2 InitialSize => new Vector2(750f, 650f);

        public VibePlayingWindow()
        {
            doCloseX = true;
            draggable = true;
            resizeable = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var comp = Current.Game?.GetComponent<VibePlayingComponent>();
            if (comp == null)
            {
                Widgets.Label(inRect, "VibePlaying_NoGame".Translate());
                return;
            }

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Title
            Text.Font = GameFont.Medium;
            listing.Label("VibePlaying_Title".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();

            // Strategy input
            listing.Label("VibePlaying_StrategyPrompt".Translate());
            inputText = listing.TextEntry(inputText, 3);
            listing.Gap(4f);

            var buttonRect = listing.GetRect(30f);
            var thirdW = buttonRect.width / 3f;
            var analyzeRect = new Rect(buttonRect.x, buttonRect.y, thirdW - 2f, 30f);
            var presetsRect = new Rect(buttonRect.x + thirdW, buttonRect.y, thirdW - 2f, 30f);
            var clearRect = new Rect(buttonRect.x + thirdW * 2, buttonRect.y, thirdW - 2f, 30f);

            bool analyzing = comp.IsAnalyzing;
            if (analyzing) GUI.color = Color.gray;
            if (Widgets.ButtonText(analyzeRect, analyzing
                ? "VibePlaying_Analyzing".Translate()
                : "VibePlaying_Analyze".Translate()) && !analyzing)
            {
                comp.TriggerAnalysis(inputText);
            }
            GUI.color = Color.white;

            if (Widgets.ButtonText(presetsRect, "VibePlaying_Presets".Translate()))
            {
                ShowPresetMenu();
            }

            if (Widgets.ButtonText(clearRect, "VibePlaying_ClearResolved".Translate()))
            {
                comp.ClearResolved();
            }

            listing.Gap(8f);

            // Tab bar
            var tabRect = listing.GetRect(30f);
            DrawTabBar(tabRect, comp);

            listing.Gap(4f);
            listing.End();

            // Content area
            float contentTop = listing.CurHeight + inRect.y;
            var contentRect = new Rect(inRect.x, contentTop, inRect.width, inRect.yMax - contentTop);

            switch (activeTab)
            {
                case 0: DrawAnalysisHistory(contentRect, comp); break;
                case 1: DrawPendingActions(contentRect, comp); break;
                case 2: DrawExecutionLog(contentRect, comp); break;
            }
        }

        private void DrawTabBar(Rect rect, VibePlayingComponent comp)
        {
            float tabW = rect.width / 3f;
            var tabs = new[]
            {
                "VibePlaying_TabAnalysis".Translate().ToString(),
                $"{"VibePlaying_TabActions".Translate()} ({comp.PendingActions.Count(a => a.Status == ActionStatus.Pending)})",
                "VibePlaying_TabLog".Translate().ToString()
            };

            for (int i = 0; i < 3; i++)
            {
                var tabR = new Rect(rect.x + i * tabW, rect.y, tabW - 2f, rect.height);
                if (activeTab == i)
                    Widgets.DrawBoxSolid(tabR, new Color(0.2f, 0.3f, 0.4f));

                if (Widgets.ButtonText(tabR, tabs[i]))
                    activeTab = i;
            }
        }

        // ==================== Tab 0: Analysis History ====================

        private void DrawAnalysisHistory(Rect rect, VibePlayingComponent comp)
        {
            var history = comp.History;
            bool streaming = LLMClient.IsStreaming;
            var streamText = streaming ? LLMClient.StreamingText : null;

            if (!streaming && history.Count == 0)
            {
                Widgets.Label(rect, "VibePlaying_NoHistory".Translate());
                return;
            }

            float contentHeight = 0f;

            // Calculate streaming block height
            float streamHeight = 0f;
            if (streaming && !string.IsNullOrEmpty(streamText))
            {
                streamHeight = 24f + Text.CalcHeight(streamText, rect.width - 24f) + 10f;
                contentHeight += streamHeight;
            }
            else if (streaming)
            {
                streamHeight = 40f;
                contentHeight += streamHeight;
            }

            foreach (var record in history)
                contentHeight += CalcRecordHeight(record, rect.width - 20f) + 10f;

            var viewRect = new Rect(0, 0, rect.width - 16f, contentHeight);
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            float y = 0f;

            // Draw live streaming block
            if (streaming)
            {
                var streamRect = new Rect(0, y, viewRect.width, streamHeight);
                Widgets.DrawBoxSolid(streamRect, new Color(0.15f, 0.2f, 0.15f, 0.4f));

                Text.Font = GameFont.Tiny;
                GUI.color = Color.cyan;
                Widgets.Label(new Rect(4f, y + 2f, viewRect.width - 8f, 20f),
                    "VibePlaying_Analyzing".Translate().ToString() + " ▌");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                if (!string.IsNullOrEmpty(streamText))
                {
                    var bodyRect = new Rect(4f, y + 22f, viewRect.width - 8f, streamHeight - 24f);
                    Widgets.Label(bodyRect, streamText);
                }

                y += streamHeight + 10f;
            }

            for (int i = 0; i < history.Count; i++)
            {
                var record = history[i];
                float height = CalcRecordHeight(record, viewRect.width);
                var entryRect = new Rect(0, y, viewRect.width, height);

                if (i % 2 == 0)
                    Widgets.DrawBoxSolid(entryRect, new Color(0.1f, 0.1f, 0.1f, 0.3f));

                // Header
                var headerRect = new Rect(entryRect.x + 4f, entryRect.y + 2f, entryRect.width - 8f, 20f);
                var tickLabel = $"Day {record.Tick / 60000}";
                if (!string.IsNullOrEmpty(record.Prompt))
                    tickLabel += $" — {record.Prompt.Substring(0, Mathf.Min(record.Prompt.Length, 60))}";
                if (record.ActionCount > 0)
                    tickLabel += $" [{record.ActionCount} actions]";

                Text.Font = GameFont.Tiny;
                GUI.color = record.IsError ? Color.red : Color.gray;
                Widgets.Label(headerRect, tickLabel);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                var bodyRect = new Rect(entryRect.x + 4f, entryRect.y + 22f, entryRect.width - 8f, height - 24f);
                GUI.color = record.IsError ? new Color(1f, 0.6f, 0.6f) : Color.white;
                Widgets.Label(bodyRect, record.Response);
                GUI.color = Color.white;

                y += height + 10f;
            }
            Widgets.EndScrollView();
        }

        // ==================== Tab 1: Pending Actions ====================

        private void DrawPendingActions(Rect rect, VibePlayingComponent comp)
        {
            var actions = comp.PendingActions;
            if (actions.Count == 0)
            {
                Widgets.Label(rect, "VibePlaying_NoActions".Translate());
                return;
            }

            // Approve All button
            var approveAllRect = new Rect(rect.x, rect.y, 150f, 28f);
            if (Widgets.ButtonText(approveAllRect, "VibePlaying_ApproveAll".Translate()))
                comp.ApproveAll();

            float listTop = rect.y + 34f;
            var listRect = new Rect(rect.x, listTop, rect.width, rect.yMax - listTop);
            float contentHeight = actions.Count * 60f;
            var viewRect = new Rect(0, 0, listRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
            float y = 0f;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var entryRect = new Rect(0, y, viewRect.width, 55f);

                // Background color by status
                Color bgColor;
                switch (action.Status)
                {
                    case ActionStatus.Executed: bgColor = new Color(0.1f, 0.3f, 0.1f, 0.3f); break;
                    case ActionStatus.Failed: bgColor = new Color(0.3f, 0.1f, 0.1f, 0.3f); break;
                    case ActionStatus.Rejected: bgColor = new Color(0.2f, 0.2f, 0.2f, 0.3f); break;
                    default: bgColor = new Color(0.15f, 0.15f, 0.2f, 0.3f); break;
                }
                Widgets.DrawBoxSolid(entryRect, bgColor);

                // Type badge
                Text.Font = GameFont.Tiny;
                GUI.color = Color.cyan;
                Widgets.Label(new Rect(4f, y + 2f, 120f, 18f), action.Type);
                GUI.color = Color.white;

                // Status badge
                string statusStr;
                Color statusColor;
                switch (action.Status)
                {
                    case ActionStatus.Executed: statusStr = "DONE"; statusColor = Color.green; break;
                    case ActionStatus.Failed: statusStr = "FAIL"; statusColor = Color.red; break;
                    case ActionStatus.Rejected: statusStr = "SKIP"; statusColor = Color.gray; break;
                    default: statusStr = "PENDING"; statusColor = Color.yellow; break;
                }
                GUI.color = statusColor;
                Widgets.Label(new Rect(130f, y + 2f, 80f, 18f), statusStr);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Description
                Widgets.Label(new Rect(4f, y + 20f, entryRect.width - 120f, 16f), action.Description ?? "");

                // Result message
                if (!string.IsNullOrEmpty(action.ResultMessage))
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = action.Status == ActionStatus.Failed ? Color.red : Color.green;
                    Widgets.Label(new Rect(4f, y + 36f, entryRect.width - 120f, 16f), action.ResultMessage);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }

                // Approve/Reject buttons (only for pending)
                if (action.Status == ActionStatus.Pending)
                {
                    float btnX = entryRect.width - 110f;
                    if (Widgets.ButtonText(new Rect(btnX, y + 8f, 50f, 22f), "✓"))
                        comp.ApproveAction(i);
                    if (Widgets.ButtonText(new Rect(btnX + 55f, y + 8f, 50f, 22f), "✗"))
                        comp.RejectAction(i);
                }

                y += 60f;
            }
            Widgets.EndScrollView();
        }

        // ==================== Tab 2: Execution Log ====================

        private void DrawExecutionLog(Rect rect, VibePlayingComponent comp)
        {
            var log = comp.ExecutionLog;
            if (log.Count == 0)
            {
                Widgets.Label(rect, "VibePlaying_NoLog".Translate());
                return;
            }

            float contentHeight = log.Count * 40f;
            var viewRect = new Rect(0, 0, rect.width - 16f, contentHeight);

            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);
            float y = 0f;
            for (int i = 0; i < log.Count; i++)
            {
                var entry = log[i];
                var entryRect = new Rect(0, y, viewRect.width, 36f);

                if (i % 2 == 0)
                    Widgets.DrawBoxSolid(entryRect, new Color(0.1f, 0.1f, 0.1f, 0.2f));

                // Day + status icon
                Text.Font = GameFont.Tiny;
                GUI.color = entry.Success ? Color.green : Color.red;
                Widgets.Label(new Rect(4f, y + 2f, entryRect.width - 8f, 16f),
                    $"[Day {entry.Tick / 60000}] {(entry.Success ? "✓" : "✗")} [{entry.ActionType}] {entry.Description}");

                GUI.color = entry.Success ? new Color(0.7f, 0.9f, 0.7f) : new Color(0.9f, 0.7f, 0.7f);
                Widgets.Label(new Rect(4f, y + 18f, entryRect.width - 8f, 16f), entry.Message);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                y += 40f;
            }
            Widgets.EndScrollView();
        }

        // ==================== Helpers ====================

        private float CalcRecordHeight(AnalysisRecord record, float width)
        {
            float textHeight = Text.CalcHeight(record.Response ?? "", width - 8f);
            return 24f + textHeight;
        }

        private void ShowPresetMenu()
        {
            var options = new[]
            {
                ("VibePlaying_Preset_Food", "Analyze food production. Are we prepared for winter? Suggest improvements."),
                ("VibePlaying_Preset_Defense", "Analyze colony defenses. Are we ready for raids? Suggest improvements."),
                ("VibePlaying_Preset_Expand", "Suggest next expansion priorities: buildings, research, recruitment."),
                ("VibePlaying_Preset_WorkOpt", "Analyze work priorities and suggest optimal assignments for all colonists."),
                ("VibePlaying_Preset_Full", "Full colony analysis with concrete actions I can approve.")
            };

            Find.WindowStack.Add(new FloatMenu(
                options.Select(o => new FloatMenuOption(o.Item1.Translate(), () => { inputText = o.Item2; })).ToList()
            ));
        }
    }
}
