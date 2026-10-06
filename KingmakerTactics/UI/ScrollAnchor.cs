namespace KingmakerTactics.UI {
    public struct ScrollAnchorResult {
        public float ScrollY;
        public float PadTop;
        public float PadBottom;
    }

    /// <summary>
    /// Keeps a card at its screen position after the list was rebuilt around it (pure,
    /// unit-testable). Scrolls the content first; where the scroll range ends, extra top or
    /// bottom padding makes up the rest — the rule list's Up/Down must leave the moved card
    /// (and its arrows) under the cursor, or the next click hits the swapped neighbour.
    /// </summary>
    public static class ScrollAnchor {
        /// <param name="scrollY">content.anchoredPosition.y of a top-pivot content (0 = top,
        /// grows as the list scrolls down).</param>
        /// <param name="cardShift">How far the card moved down in content space through the
        /// rebuild (negative = up).</param>
        /// <param name="contentHeight">Content height after the rebuild, without extra padding.</param>
        public static ScrollAnchorResult Keep(float scrollY, float cardShift, float contentHeight, float viewportHeight) {
            float target = scrollY + cardShift;
            if (target < 0f)
                return new ScrollAnchorResult { ScrollY = 0f, PadTop = -target };
            float maxScroll = contentHeight - viewportHeight;
            if (target > maxScroll && target > 0f)
                return new ScrollAnchorResult { ScrollY = target, PadBottom = target - maxScroll };
            return new ScrollAnchorResult { ScrollY = target };
        }
    }
}
