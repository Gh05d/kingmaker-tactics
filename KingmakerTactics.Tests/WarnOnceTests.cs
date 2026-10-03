using KingmakerTactics.Engine;
using Xunit;

namespace KingmakerTactics.Tests {
    public class WarnOnceTests {
        [Fact]
        public void Same_unit_and_rule_warns_once() {
            var w = new WarnOnce();
            Assert.True(w.ShouldWarn("u1", "r1"));
            Assert.False(w.ShouldWarn("u1", "r1"));
        }

        [Fact]
        public void Other_unit_or_rule_warns_independently() {
            var w = new WarnOnce();
            Assert.True(w.ShouldWarn("u1", "r1"));
            Assert.True(w.ShouldWarn("u2", "r1"));
            Assert.True(w.ShouldWarn("u1", "r2"));
        }

        // Reset at every combat start/end: each fight reports its unusable rules again.
        [Fact]
        public void Reset_rearms_the_warning() {
            var w = new WarnOnce();
            w.ShouldWarn("u1", "r1");
            w.Reset();
            Assert.True(w.ShouldWarn("u1", "r1"));
        }
    }
}
