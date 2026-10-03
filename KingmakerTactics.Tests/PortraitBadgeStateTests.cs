using KingmakerTactics.UI;
using Xunit;

namespace KingmakerTactics.Tests {
    public class PortraitBadgeStateTests {
        [Fact]
        public void Hidden_when_option_off() =>
            Assert.Equal(PortraitBadgeState.Hidden, PortraitBadges.StateFor(show: false, unitId: "u1", isEnabled: _ => true));

        // Empty party slots / unbound console views carry no unit.
        [Fact]
        public void Hidden_without_unit() =>
            Assert.Equal(PortraitBadgeState.Hidden, PortraitBadges.StateFor(show: true, unitId: null, isEnabled: _ => true));

        // Deck screenshot 2026-10-03: anchored to the cell corner, the badge sat on the gold
        // frame and the HP bar. It is placed from the portrait IMAGE's top-left corner instead.
        [Fact]
        public void Badge_center_sits_inset_inside_the_portrait_corner() {
            var c = PortraitBadges.CenterFromTopLeft(new UnityEngine.Vector2(10f, 50f), size: 26f, inset: 2f);
            Assert.Equal(10f + 2f + 13f, c.x, 3);
            Assert.Equal(50f - 2f - 13f, c.y, 3);
        }

        [Fact]
        public void Reflects_per_unit_tactics_switch() {
            Assert.Equal(PortraitBadgeState.On, PortraitBadges.StateFor(true, "u1", id => id == "u1"));
            Assert.Equal(PortraitBadgeState.Off, PortraitBadges.StateFor(true, "u2", id => id == "u1"));
        }
    }
}
