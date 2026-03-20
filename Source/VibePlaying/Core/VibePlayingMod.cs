using UnityEngine;
using Verse;

namespace VibePlaying
{
    public class VibePlayingMod : Mod
    {
        public static VibePlayingSettings Settings { get; private set; }

        public VibePlayingMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<VibePlayingSettings>();
        }

        public override string SettingsCategory() => "VibePlaying";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // --- API Configuration ---
            listing.Label("VibePlaying_Settings_APISection".Translate());
            listing.GapLine();

            Settings.apiEndpoint = listing.TextEntryLabeled(
                "VibePlaying_Settings_Endpoint".Translate(), Settings.apiEndpoint);

            Settings.apiKey = listing.TextEntryLabeled(
                "VibePlaying_Settings_APIKey".Translate(), Settings.apiKey);

            Settings.model = listing.TextEntryLabeled(
                "VibePlaying_Settings_Model".Translate(), Settings.model);

            listing.Label("VibePlaying_Settings_MaxTokens".Translate() + $": {Settings.maxTokens}");
            Settings.maxTokens = (int)listing.Slider(Settings.maxTokens, 512, 16384);

            listing.Label("VibePlaying_Settings_Temperature".Translate() + $": {Settings.temperature:F2}");
            Settings.temperature = listing.Slider(Settings.temperature, 0f, 1.5f);

            listing.Label("VibePlaying_Settings_Timeout".Translate() + $": {Settings.timeoutSeconds}s");
            Settings.timeoutSeconds = (int)listing.Slider(Settings.timeoutSeconds, 5, 120);

            listing.Gap();

            // --- Automation ---
            listing.Label("VibePlaying_Settings_AutoSection".Translate());
            listing.GapLine();

            listing.CheckboxLabeled(
                "VibePlaying_Settings_AutoAnalyze".Translate(), ref Settings.autoAnalyze);

            listing.Label("VibePlaying_Settings_CycleDays".Translate() + $": {Settings.analysisCycleDays}");
            Settings.analysisCycleDays = (int)listing.Slider(Settings.analysisCycleDays, 1, 15);

            listing.Gap();

            // --- Auto-execution per action type ---
            listing.Label("VibePlaying_Settings_AutoExecSection".Translate());
            listing.GapLine();

            listing.CheckboxLabeled(
                "VibePlaying_Settings_AutoExecWork".Translate(), ref Settings.autoExecWorkPriority);
            listing.CheckboxLabeled(
                "VibePlaying_Settings_AutoExecBlueprint".Translate(), ref Settings.autoExecBlueprint);
            listing.CheckboxLabeled(
                "VibePlaying_Settings_AutoExecDesignate".Translate(), ref Settings.autoExecDesignate);
            listing.CheckboxLabeled(
                "VibePlaying_Settings_AutoExecBill".Translate(), ref Settings.autoExecBill);
            listing.CheckboxLabeled(
                "VibePlaying_Settings_AutoExecReport".Translate(), ref Settings.autoExecReport);

            listing.Gap();

            // --- Safety ---
            listing.Label("VibePlaying_Settings_SafetySection".Translate());
            listing.GapLine();

            listing.Label("VibePlaying_Settings_MaxBlueprints".Translate() + $": {Settings.maxBlueprintsPerCycle}");
            Settings.maxBlueprintsPerCycle = (int)listing.Slider(Settings.maxBlueprintsPerCycle, 5, 100);

            listing.Label("VibePlaying_Settings_MaxDesignations".Translate() + $": {Settings.maxDesignationsPerCycle}");
            Settings.maxDesignationsPerCycle = (int)listing.Slider(Settings.maxDesignationsPerCycle, 5, 200);

            listing.Label("VibePlaying_Settings_MaxBills".Translate() + $": {Settings.maxBillsPerCycle}");
            Settings.maxBillsPerCycle = (int)listing.Slider(Settings.maxBillsPerCycle, 5, 50);

            listing.Label("VibePlaying_Settings_MaxWorkChanges".Translate() + $": {Settings.maxWorkChangesPerCycle}");
            Settings.maxWorkChangesPerCycle = (int)listing.Slider(Settings.maxWorkChangesPerCycle, 5, 50);

            listing.End();
        }
    }
}
