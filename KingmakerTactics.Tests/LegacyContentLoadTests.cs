using KingmakerTactics.Models;
using KingmakerTactics.Persistence;
using Newtonsoft.Json;
using Xunit;

namespace KingmakerTactics.Tests {
    public class LegacyContentLoadTests {
        static JsonSerializerSettings Settings() {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new SafeConditionConverter());
            return settings;
        }

        // A Wrath-era condition whose enum value does not exist in this build must be
        // dropped (null), not throw — packs/presets copied from Wrath Tactics hit this.
        [Fact]
        public void UnknownConditionPropertyIsDropped() {
            var json = "{\"Subject\":\"Self\",\"Property\":\"ThisValueDoesNotExistInKingmaker\",\"Operator\":\"Equal\",\"Value\":\"1\"}";
            var cond = JsonConvert.DeserializeObject<Condition>(json, Settings());
            Assert.Null(cond);
        }

        [Fact]
        public void KnownConditionStillLoads() {
            var json = "{\"Subject\":\"Self\",\"Property\":\"HpPercent\",\"Operator\":\"LessThan\",\"Value\":\"50\"}";
            var cond = JsonConvert.DeserializeObject<Condition>(json, Settings());
            Assert.NotNull(cond);
            Assert.Equal("50", cond.Value);
        }
    }
}
