using System.Collections.Generic;
using System.Linq;
using KingmakerTactics.Engine;
using Xunit;

namespace KingmakerTactics.Tests {
    public class DefaultPacksTests {
        [Fact]
        public void Every_pack_member_is_a_default_preset() {
            var presetIds = new HashSet<string>(DefaultPresets.Build().Select(p => p.Id));
            foreach (var pack in DefaultPacks.Build())
                foreach (var id in pack.PresetIds)
                    Assert.True(presetIds.Contains(id), $"{pack.Name}: unknown preset {id}");
        }

        [Fact]
        public void Default_ids_are_unique() {
            var presets = DefaultPresets.Build().Select(p => p.Id).ToList();
            Assert.Equal(presets.Count, presets.Distinct().Count());
            var packs = DefaultPacks.Build().Select(p => p.Id).ToList();
            Assert.Equal(packs.Count, packs.Distinct().Count());
        }

        [Fact]
        public void Six_role_packs_none_empty() {
            var packs = DefaultPacks.Build();
            Assert.Equal(6, packs.Count);
            Assert.All(packs, p => Assert.NotEmpty(p.PresetIds));
        }
    }

    public class DefaultSeedingTests {
        [Fact]
        public void First_install_writes_everything() {
            var seeded = new HashSet<string>();
            var write = DefaultSeeding.Plan(new[] { "a", "b" }, seeded, id => false);
            Assert.Equal(new[] { "a", "b" }, write);
            Assert.True(seeded.SetEquals(new[] { "a", "b" }));
        }

        // A default the user deleted stays deleted: its id is in the sentinel.
        [Fact]
        public void Already_seeded_ids_are_never_rewritten() {
            var seeded = new HashSet<string> { "a" };
            var write = DefaultSeeding.Plan(new[] { "a", "b" }, seeded, id => false);
            Assert.Equal(new[] { "b" }, write);
        }

        // Upgrade from a pre-sentinel version: the file exists, so only mark it seeded.
        [Fact]
        public void Existing_files_are_marked_but_not_rewritten() {
            var seeded = new HashSet<string>();
            var write = DefaultSeeding.Plan(new[] { "a" }, seeded, id => id == "a");
            Assert.Empty(write);
            Assert.Contains("a", seeded);
        }
    }
}
