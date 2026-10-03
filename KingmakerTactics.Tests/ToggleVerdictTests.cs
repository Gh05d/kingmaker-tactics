using KingmakerTactics.Engine;
using KingmakerTactics.Models;
using Xunit;

namespace KingmakerTactics.Tests {
    // Deck 2026-10-03: "Inspire Courage On" logged WARN "not executable" while the song was
    // already running — the success state. The validator must tell the cases apart.
    public class ToggleVerdictTests {
        [Fact]
        public void Not_owned_when_unit_lacks_the_activatable() =>
            Assert.Equal(ToggleVerdict.NotOwned, ActionValidator.JudgeToggle(owned: false, isOn: false, isAvailable: false, mode: ToggleMode.On));

        [Fact]
        public void Already_on_is_its_own_verdict() =>
            Assert.Equal(ToggleVerdict.AlreadyInState, ActionValidator.JudgeToggle(true, isOn: true, isAvailable: true, mode: ToggleMode.On));

        [Fact]
        public void Already_off_is_its_own_verdict() =>
            Assert.Equal(ToggleVerdict.AlreadyInState, ActionValidator.JudgeToggle(true, isOn: false, isAvailable: true, mode: ToggleMode.Off));

        [Fact]
        public void Off_but_unavailable() =>
            Assert.Equal(ToggleVerdict.Unavailable, ActionValidator.JudgeToggle(true, isOn: false, isAvailable: false, mode: ToggleMode.On));

        [Fact]
        public void Ok_both_directions() {
            Assert.Equal(ToggleVerdict.Ok, ActionValidator.JudgeToggle(true, isOn: false, isAvailable: true, mode: ToggleMode.On));
            Assert.Equal(ToggleVerdict.Ok, ActionValidator.JudgeToggle(true, isOn: true, isAvailable: false, mode: ToggleMode.Off));
        }
    }
}
