using KingmakerTactics.Compatibility;
using Xunit;

namespace KingmakerTactics.Tests {
    // Shape of KingmakerBuffBot.Main: public static field `settings` holding a public bool
    // field `castCombatStart` (decompiled Buff Bot King2.0).
    public class FakeBuffBotSettings { public bool castCombatStart; }
    public static class FakeBuffBotMain { public static FakeBuffBotSettings settings; }
    public static class FakeBuffBotWithoutSettings { }

    public class BuffBotCompatTests {
        [Fact]
        public void Reads_cast_on_combat_start_from_settings() {
            FakeBuffBotMain.settings = new FakeBuffBotSettings { castCombatStart = true };
            Assert.True(BuffBotCompat.ReadCastCombatStart(typeof(FakeBuffBotMain)));
            FakeBuffBotMain.settings.castCombatStart = false;
            Assert.False(BuffBotCompat.ReadCastCombatStart(typeof(FakeBuffBotMain)));
        }

        // Before Buff Bot's Load ran (settings still null) or with a changed layout: unknown.
        [Fact]
        public void Unknown_when_settings_missing() {
            FakeBuffBotMain.settings = null;
            Assert.Null(BuffBotCompat.ReadCastCombatStart(typeof(FakeBuffBotMain)));
            Assert.Null(BuffBotCompat.ReadCastCombatStart(typeof(FakeBuffBotWithoutSettings)));
            Assert.Null(BuffBotCompat.ReadCastCombatStart(null));
        }
    }
}
