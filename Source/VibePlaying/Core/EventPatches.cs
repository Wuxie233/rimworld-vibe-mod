using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VibePlaying
{
    /// <summary>
    /// Harmony patches to detect important game events and trigger AI analysis.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class EventPatches
    {
        static EventPatches()
        {
            var harmony = new Harmony("wuxie.vibeplaying.events");
            harmony.PatchAll(typeof(EventPatches).Assembly);
        }
    }

    /// <summary>
    /// Detect when a raid/threat incident starts.
    /// </summary>
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class Patch_IncidentWorker_TryExecute
    {
        public static void Postfix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!__result) return; // incident didn't fire
            if (!VibePlayingMod.Settings.autoAnalyze) return;

            // Only trigger on threatening incidents
            var cat = __instance.def?.category;
            if (cat != IncidentCategoryDefOf.ThreatBig && cat != IncidentCategoryDefOf.ThreatSmall)
                return;

            var comp = Current.Game?.GetComponent<VibePlayingComponent>();
            if (comp == null || comp.IsAnalyzing) return;

            // Queue a threat-focused analysis with a 5-second delay to let the raid spawn
            var incidentLabel = __instance.def?.label ?? "threat";
            Log.Message($"[VibePlaying] Detected incident: {incidentLabel}. Scheduling threat analysis.");
            comp.QueueEventAnalysis($"URGENT: A {incidentLabel} just started! Assess threats and suggest defensive actions immediately.");
        }
    }

    /// <summary>
    /// Detect when a colonist has a mental break.
    /// </summary>
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public static class Patch_MentalBreak
    {
        public static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef, ref bool __result)
        {
            if (!__result) return;
            if (!VibePlayingMod.Settings.autoAnalyze) return;

            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn?.Faction != Faction.OfPlayer) return;
            if (!pawn.IsColonist) return;

            var comp = Current.Game?.GetComponent<VibePlayingComponent>();
            if (comp == null || comp.IsAnalyzing) return;

            var breakName = stateDef?.label ?? "mental break";
            Log.Message($"[VibePlaying] Colonist {pawn.LabelShort} — {breakName}. Scheduling mood analysis.");
            comp.QueueEventAnalysis($"ALERT: {pawn.LabelShort} is having a {breakName}! Analyze colony mood and suggest mitigation.");
        }
    }
}
