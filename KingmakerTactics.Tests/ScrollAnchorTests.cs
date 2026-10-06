using KingmakerTactics.UI;
using Xunit;

namespace KingmakerTactics.Tests {
    // Deck report 2026-10-06: after Up the moved card sat one slot higher while the cursor
    // stayed, so the second click hit the swapped neighbour's arrow and moved it back.
    // ScrollAnchor keeps the moved card's screen position: scroll first, pad where the scroll
    // range ends. Invariant: padTop - newScroll == -(oldScroll + shift).
    public class ScrollAnchorTests {
        [Fact]
        public void Scrolls_when_the_range_allows() {
            var r = ScrollAnchor.Keep(scrollY: 300f, cardShift: -120f, contentHeight: 2000f, viewportHeight: 600f);
            Assert.Equal(180f, r.ScrollY, 3);
            Assert.Equal(0f, r.PadTop, 3);
            Assert.Equal(0f, r.PadBottom, 3);
        }

        [Fact]
        public void Pads_the_top_when_the_list_is_scrolled_to_the_top() {
            var r = ScrollAnchor.Keep(scrollY: 50f, cardShift: -120f, contentHeight: 2000f, viewportHeight: 600f);
            Assert.Equal(0f, r.ScrollY, 3);
            Assert.Equal(70f, r.PadTop, 3);
            Assert.Equal(0f, r.PadBottom, 3);
        }

        [Fact]
        public void Pads_the_top_of_a_list_that_does_not_scroll() {
            var r = ScrollAnchor.Keep(scrollY: 0f, cardShift: -120f, contentHeight: 400f, viewportHeight: 600f);
            Assert.Equal(0f, r.ScrollY, 3);
            Assert.Equal(120f, r.PadTop, 3);
        }

        [Fact]
        public void Pads_the_bottom_when_moving_down_past_the_scroll_end() {
            // max scroll = 1400; target 1350 + 120 = 1470 → 70 px of bottom padding.
            var r = ScrollAnchor.Keep(scrollY: 1350f, cardShift: 120f, contentHeight: 2000f, viewportHeight: 600f);
            Assert.Equal(1470f, r.ScrollY, 3);
            Assert.Equal(0f, r.PadTop, 3);
            Assert.Equal(70f, r.PadBottom, 3);
        }

        [Fact]
        public void Pads_the_bottom_of_a_list_that_does_not_scroll() {
            // Content 400 in a 600 viewport: scrolling 120 needs content 720 → 320 px padding.
            var r = ScrollAnchor.Keep(scrollY: 0f, cardShift: 120f, contentHeight: 400f, viewportHeight: 600f);
            Assert.Equal(120f, r.ScrollY, 3);
            Assert.Equal(320f, r.PadBottom, 3);
        }

        [Fact]
        public void No_shift_changes_nothing() {
            var r = ScrollAnchor.Keep(scrollY: 200f, cardShift: 0f, contentHeight: 2000f, viewportHeight: 600f);
            Assert.Equal(200f, r.ScrollY, 3);
            Assert.Equal(0f, r.PadTop, 3);
            Assert.Equal(0f, r.PadBottom, 3);
        }
    }
}
