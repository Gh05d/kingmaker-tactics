using Kingmaker.UnitLogic;
using KingmakerTactics.Engine;
using Xunit;

namespace KingmakerTactics.Tests {
    public class HealTargetTests {
        // A dead ally cannot be targeted by Cure spells (the engine rejects the target), so as
        // "lowest HP ally" it would block healing everyone else. Unconscious allies are the most
        // valuable heal target: a Cure brings them back up.
        [Theory]
        [InlineData(UnitLifeState.Conscious, true)]
        [InlineData(UnitLifeState.Unconscious, true)]
        [InlineData(UnitLifeState.Dead, false)]
        public void Only_dead_allies_are_excluded_as_heal_targets(UnitLifeState state, bool expected) {
            Assert.Equal(expected, TargetResolver.IsHealableLifeState(state));
        }
    }
}
