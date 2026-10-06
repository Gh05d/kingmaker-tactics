using System;
using System.Diagnostics;

namespace KingmakerTactics.Logging {
    /// <summary>
    /// Attributes frame hitches to the mod's per-frame work. Sections accumulate
    /// Stopwatch ticks during a frame; at the start of the next OnUpdate the previous
    /// frame's real duration (Time.unscaledDeltaTime) is compared against what the mod
    /// spent in it and whether a GC ran. A 60 s summary goes to the session log, plus one
    /// line per slow frame in which the mod itself spent at least ModLineMs (the game's own
    /// hitches would flood the log otherwise). Allocation-free on the hot path.
    /// Triage for "the game stutters with the mod": grep 'Perf \|Slow frame' in the mod log.
    /// </summary>
    public static class FrameProbe {
        public enum Section { Tick, BuffScan, Overlay, Discover, Count }

        const float SlowFrameSeconds = 0.05f;
        const double ModLineMs = 5.0;
        const float SummarySeconds = 60f;
        const int MaxSlowLinesPerSummary = 40;

        static readonly long[] frameTicks = new long[(int)Section.Count];
        static readonly long[] maxTicks = new long[(int)Section.Count];
        static readonly long[] sumTicks = new long[(int)Section.Count];
        static int lastGcCount = -1;
        static int frames, slowFrames, gcFrames, slowWithGc, slowLines;
        static float summaryTimer, maxFrame;

        public static long Start() => Stopwatch.GetTimestamp();

        public static void Add(Section section, long start) {
            frameTicks[(int)section] += Stopwatch.GetTimestamp() - start;
        }

        static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        /// <summary>Call once at the start of OnUpdate with Time.unscaledDeltaTime: closes
        /// the previous frame, whose duration that delta is.</summary>
        public static void BeginFrame(float lastFrameSeconds, bool loading) {
            int gc = GC.CollectionCount(0);
            bool gcRan = lastGcCount >= 0 && gc != lastGcCount;
            lastGcCount = gc;

            if (!loading) {
                frames++;
                if (gcRan) gcFrames++;
                if (lastFrameSeconds > maxFrame) maxFrame = lastFrameSeconds;
                long modTicks = 0;
                for (int i = 0; i < (int)Section.Count; i++) {
                    if (i != (int)Section.Discover) modTicks += frameTicks[i]; // Discover is inside Overlay
                    sumTicks[i] += frameTicks[i];
                    if (frameTicks[i] > maxTicks[i]) maxTicks[i] = frameTicks[i];
                }
                if (lastFrameSeconds >= SlowFrameSeconds) {
                    slowFrames++;
                    if (gcRan) slowWithGc++;
                    if (Ms(modTicks) >= ModLineMs && slowLines++ < MaxSlowLinesPerSummary) {
                        Log.Engine.Info($"Slow frame {lastFrameSeconds * 1000f:F0} ms: mod {Ms(modTicks):F1} ms "
                            + $"(tick {Ms(frameTicks[(int)Section.Tick]):F1}, overlay {Ms(frameTicks[(int)Section.Overlay]):F1}"
                            + $" [discover {Ms(frameTicks[(int)Section.Discover]):F1}], scan {Ms(frameTicks[(int)Section.BuffScan]):F1})"
                            + $"{(gcRan ? ", GC ran" : "")}");
                    }
                }
                summaryTimer += lastFrameSeconds;
                if (summaryTimer >= SummarySeconds) WriteSummary();
            }
            Array.Clear(frameTicks, 0, frameTicks.Length);
        }

        static void WriteSummary() {
            Log.Engine.Info($"Perf {summaryTimer:F0}s: {frames} frames, {slowFrames} slow (>= {SlowFrameSeconds * 1000f:F0} ms, "
                + $"{slowWithGc} with GC), max frame {maxFrame * 1000f:F0} ms, GC in {gcFrames} frames; mod avg/max ms: "
                + $"tick {Ms(sumTicks[0]) / frames:F2}/{Ms(maxTicks[0]):F1}, scan {Ms(sumTicks[1]) / frames:F2}/{Ms(maxTicks[1]):F1}, "
                + $"overlay {Ms(sumTicks[2]) / frames:F2}/{Ms(maxTicks[2]):F1}, discover max {Ms(maxTicks[3]):F1}");
            Array.Clear(sumTicks, 0, sumTicks.Length);
            Array.Clear(maxTicks, 0, maxTicks.Length);
            frames = slowFrames = gcFrames = slowWithGc = slowLines = 0;
            summaryTimer = 0f;
            maxFrame = 0f;
        }
    }
}
