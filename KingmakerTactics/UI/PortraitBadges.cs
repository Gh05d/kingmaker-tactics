using System;
using UnityEngine;

namespace KingmakerTactics.UI {
    public enum PortraitBadgeState { Hidden, On, Off }

    /// <summary>Pure state rule for the portrait badge (unit-testable without Unity).</summary>
    public static class PortraitBadges {
        public static PortraitBadgeState StateFor(bool show, string unitId, Func<string, bool> isEnabled) {
            if (!show || unitId == null) return PortraitBadgeState.Hidden;
            return isEnabled(unitId) ? PortraitBadgeState.On : PortraitBadgeState.Off;
        }

        /// <summary>Center of a size×size badge whose top-left sits <paramref name="inset"/>
        /// inside the given top-left corner (y grows upward, as in Unity local space).</summary>
        public static Vector2 CenterFromTopLeft(Vector2 topLeft, float size, float inset) =>
            new Vector2(topLeft.x + inset + size * 0.5f, topLeft.y - inset - size * 0.5f);
    }
}
