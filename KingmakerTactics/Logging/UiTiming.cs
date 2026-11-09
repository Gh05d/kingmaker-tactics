using System.Diagnostics;
using UnityEngine;

namespace KingmakerTactics.Logging {
    /// <summary>
    /// Times user-triggered panel work (rule-list rebuilds, panel open, pickers). FrameProbe
    /// only sees what runs inside Main.OnUpdate; button handlers and the panel's own Update
    /// run outside it, and Unity's layout/TMP mesh pass for freshly built widgets runs later
    /// in the same frame. So every timed operation logs its own duration, and the first
    /// FrameProbe.BeginFrame after it logs the real length of that frame (which includes the
    /// layout cost). Triage for "the menu freezes": grep 'Timing \|Frame with ' in the mod log.
    /// Low volume — one line per user action, never per frame.
    /// </summary>
    public static class UiTiming {
        static int depth;
        static int pendingFrame = -1;
        static double pendingMs;
        static string pendingWhat;

        /// <summary>Time spent building spell/ability lists for rule cards (RuleEditorWidget.GetSpellEntries).</summary>
        public static long SpellListTicks;
        public static int SpellListCalls;

        public static long Start() {
            depth++;
            return Stopwatch.GetTimestamp();
        }

        public static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        public static void End(string what, long start, string detail = "") {
            double ms = Ms(Stopwatch.GetTimestamp() - start);
            depth--;
            Log.UI.Info($"Timing {what}: {ms:F1} ms{detail}");
            if (depth > 0) return; // nested inside another timed operation, already counted there
            if (depth < 0) depth = 0;
            int frame = Time.frameCount;
            if (pendingFrame == frame) {
                pendingMs += ms;
                pendingWhat += " + " + what;
            } else {
                pendingFrame = frame;
                pendingMs = ms;
                pendingWhat = what;
            }
        }

        /// <summary>Called from FrameProbe.BeginFrame with the previous frame's duration.</summary>
        internal static void OnFrameClosed(float lastFrameSeconds) {
            if (pendingFrame < 0 || Time.frameCount <= pendingFrame) return;
            Log.UI.Info($"Frame with {pendingWhat}: {lastFrameSeconds * 1000f:F0} ms total, {pendingMs:F1} ms in panel code");
            pendingFrame = -1;
        }
    }
}
