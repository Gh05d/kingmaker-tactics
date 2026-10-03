using KingmakerTactics.Logging;
using TurnBased.Controllers;

namespace KingmakerTactics.Engine {
    // v1.0 is real-time only (turn-based support dropped 2026-10-03): while a turn-based
    // combat runs we issue nothing. Polled per tick instead of tracked via
    // ITurnBasedModeEnabledHandler: the static CombatController.IsInTurnBasedCombat()
    // (KM IL: Player.IsInCombat && setting EnableTurnBasedMode && Game.CurrentMode != Cutscene;
    // there is no TurnBased game mode) is also correct right after loading a save that is
    // already in turn-based combat.
    internal static class TurnBasedGate {
        internal class State {
            bool last;

            public bool Observe(bool turnBasedNow, out bool changed) {
                changed = turnBasedNow != last;
                last = turnBasedNow;
                return turnBasedNow;
            }
        }

        static readonly State state = new State();

        public static bool IsActive() {
            bool active = state.Observe(CombatController.IsInTurnBasedCombat(), out bool changed);
            if (changed) {
                Log.Engine.Info(active
                    ? "Turn-based mode on — tactics paused (TB support is not implemented yet)"
                    : "Turn-based mode off — tactics resumed");
            }
            return active;
        }
    }
}
