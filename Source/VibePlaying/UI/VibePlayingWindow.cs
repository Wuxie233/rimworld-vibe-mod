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

        public override Vector2 InitialSize => new Vector2(700f, 600f);

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
            var leftButton = buttonRect.LeftHalf();
            var rightButton = buttonRect.RightHalf();

            // Analyze button
            bool analyzing = comp.IsAnalyzing;
            if (analyzing)
                GUI.color = Color.gray;

            if (Widgets.ButtonText(leftButton, analyzing
                ? "VibePlaying_Analyzing".Translate()
                : "VibePlaying_Analyze".Translate()) && !analyzing)
            {
                comp.TriggerAnalysis(inputText);
            }

            GUI.color = Color.white;

            // Quick presets
            if (Widgets.ButtonText(rightButton, "VibePlaying_Presets".Translate()))
            {
                var options = new[]
                {
                    ("VibePlaying_Preset_Food", "Analyze food production. Are we prepared for winter? Suggest improvements."),
                    ("VibePlaying_Preset_Defense", "Analyze colony defenses. Are we ready for raids? Suggest improvements."),
                    ("VibePlaying_Preset_Expand", "Suggest next expansion priorities: buildings, research, recruitment."),
                    ("VibePlaying_Preset_WorkOpt", "Analyze work priorities. Are colonists assigned optimally based on skills?"),
                    ("VibePlaying_Preset_Full", "Full colony analysis and strategic recommendations.")
                };

                Find.WindowStack.Add(new FloatMenu(
                    options.Select(o => new FloatMenuOption(o.Item1.Translate(), () => { inputText = o.Item2; })).ToList()
                ));
            }

            listing.Gap(8f);
            listing.GapLine();

            // Status
            if (analyzing)
            {
                listing.Label("VibePlaying_WaitingResponse".Translate());
                listing.Gap();
            }

            // History
            listing.Label("VibePlaying_History".Translate());
            listing.Gap(4f);
            listing.End();

            // Scrollable history area
            float historyTop = listing.CurHeight + inRect.y;
            var historyRect = new Rect(inRect.x, historyTop, inRect.width, inRect.yMax - historyTop);
            DrawHistory(historyRect, comp);
        }

        private void DrawHistory(Rect rect, VibePlayingComponent comp)
        {
            var history = comp.History;
            if (history.Count == 0)
            {
                Widgets.Label(rect, "VibePlaying_NoHistory".Translate());
                return;
            }

            float contentHeight = 0f;
            foreach (var record in history)
                contentHeight += CalcRecordHeight(record, rect.width - 20f) + 10f;

            var viewRect = new Rect(0, 0, rect.width - 16f, contentHeight);
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            float y = 0f;
            for (int i = 0; i < history.Count; i++)
            {
                var record = history[i];
                float height = CalcRecordHeight(record, viewRect.width);
                var entryRect = new Rect(0, y, viewRect.width, height);

                // Background
                if (i % 2 == 0)
                    Widgets.DrawBoxSolid(entryRect, new Color(0.1f, 0.1f, 0.1f, 0.3f));

                // Header
                var headerRect = new Rect(entryRect.x + 4f, entryRect.y + 2f, entryRect.width - 8f, 20f);
                var tickLabel = $"Day {record.Tick / 60000}";
                if (!string.IsNullOrEmpty(record.Prompt))
                    tickLabel += $" — {record.Prompt.Substring(0, Mathf.Min(record.Prompt.Length, 60))}";

                Text.Font = GameFont.Tiny;
                GUI.color = record.IsError ? Color.red : Color.gray;
                Widgets.Label(headerRect, tickLabel);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Response body
                var bodyRect = new Rect(entryRect.x + 4f, entryRect.y + 22f, entryRect.width - 8f, height - 24f);
                GUI.color = record.IsError ? new Color(1f, 0.6f, 0.6f) : Color.white;
                Widgets.Label(bodyRect, record.Response);
                GUI.color = Color.white;

                y += height + 10f;
            }

            Widgets.EndScrollView();
        }

        private float CalcRecordHeight(AnalysisRecord record, float width)
        {
            float textHeight = Text.CalcHeight(record.Response ?? "", width - 8f);
            return 24f + textHeight;
        }
    }
}
