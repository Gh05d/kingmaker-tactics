using System;

namespace KingmakerTactics.UI {
    public enum PortraitBadgeState { Hidden, On, Off }

    /// <summary>Pure state rule for the portrait badge (unit-testable without Unity).</summary>
    public static class PortraitBadges {
        public static PortraitBadgeState StateFor(bool show, string unitId, Func<string, bool> isEnabled) {
            if (!show || unitId == null) return PortraitBadgeState.Hidden;
            return isEnabled(unitId) ? PortraitBadgeState.On : PortraitBadgeState.Off;
        }
    }
}
