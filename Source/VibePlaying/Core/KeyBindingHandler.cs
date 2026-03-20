using RimWorld;
using UnityEngine;
using Verse;

namespace VibePlaying
{
    [DefOf]
    public static class VibePlayingKeyBindingDefOf
    {
        public static KeyBindingDef VibePlaying_OpenWindow;
        public static KeyBindingDef VibePlaying_QuickAnalyze;
    }

    /// <summary>
    /// Handles global keyboard shortcuts via GameComponent.
    /// </summary>
    public class KeyBindingHandler : GameComponent
    {
        public KeyBindingHandler(Game game) { }

        public override void GameComponentOnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            if (Event.current.type != EventType.KeyDown) return;

            if (VibePlayingKeyBindingDefOf.VibePlaying_OpenWindow.JustPressed)
            {
                if (!Find.WindowStack.IsOpen<VibePlayingWindow>())
                    Find.WindowStack.Add(new VibePlayingWindow());
                else
                    Find.WindowStack.TryRemove(typeof(VibePlayingWindow));
                Event.current.Use();
            }

            if (VibePlayingKeyBindingDefOf.VibePlaying_QuickAnalyze.JustPressed)
            {
                var comp = Current.Game?.GetComponent<VibePlayingComponent>();
                if (comp != null && !comp.IsAnalyzing)
                {
                    comp.TriggerAnalysis(null);
                    Messages.Message("VibePlaying: Analysis started", MessageTypeDefOf.SilentInput);
                }
                Event.current.Use();
            }
        }
    }
}
