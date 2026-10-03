using KingmakerTactics.Engine;
using Xunit;

namespace KingmakerTactics.Tests {
    public class TurnBasedGateTests {
        [Fact]
        public void ReportsEachTransitionExactlyOnce() {
            var gate = new TurnBasedGate.State();
            Assert.True(gate.Observe(true, out bool changed));
            Assert.True(changed);
            Assert.True(gate.Observe(true, out changed));
            Assert.False(changed);
            Assert.False(gate.Observe(false, out changed));
            Assert.True(changed);
            Assert.False(gate.Observe(false, out changed));
            Assert.False(changed);
        }

        [Fact]
        public void StartsInRealTime() {
            var gate = new TurnBasedGate.State();
            Assert.False(gate.Observe(false, out bool changed));
            Assert.False(changed);
        }
    }
}
