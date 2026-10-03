using System;
using KingmakerTactics.Logging;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Builds the authoritative buff list once per session for the HasBuff/MissingBuff picker.
    ///
    /// Wrath needed a chunked force-load of the lazily-populated BlueprintsCache here.
    /// Kingmaker keeps every blueprint resident once the LibraryScene is loaded
    /// (engine-verification.md §13: ResourcesLibrary.GetBlueprints&lt;T&gt;() enumerates the full
    /// library), so a single enumeration is complete. Same entry points as Wrath
    /// (EnsureStarted/Pump from Main.OnUpdate) to keep the call sites unchanged.
    /// </summary>
    public static class BuffPackScanner {
        static bool completed;

        public static bool Completed => completed;
        public static bool InProgress => false;

        /// <summary>Idempotent. Call from OnUpdate after the Game.Instance.Player guard.</summary>
        public static void EnsureStarted() {
            if (completed) return;
            completed = true;
            try {
                BuffBlueprintProvider.OnFullScanComplete();
            } catch (Exception ex) {
                Log.Engine.Error(ex, "BuffPackScanner: buff enumeration failed");
            }
        }

        public static void Pump() { }
    }
}
