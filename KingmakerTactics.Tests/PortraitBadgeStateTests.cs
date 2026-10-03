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

        [Fact]
        public void Reflects_per_unit_tactics_switch() {
            Assert.Equal(PortraitBadgeState.On, PortraitBadges.StateFor(true, "u1", id => id == "u1"));
            Assert.Equal(PortraitBadgeState.Off, PortraitBadges.StateFor(true, "u2", id => id == "u1"));
        }
    }
}
