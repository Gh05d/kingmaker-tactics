using System.Collections.Generic;
using System.Linq;
using KingmakerTactics.Engine;
using KingmakerTactics.Models;
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
        public void Six_role_packs_each_with_a_real_game_plan() {
            var packs = DefaultPacks.Build();
            Assert.Equal(6, packs.Count);
            Assert.All(packs, p => Assert.True(p.PresetIds.Count >= 4, $"{p.Name}: only {p.PresetIds.Count} rules"));
        }

        static readonly HashSet<string> HealGuids = new HashSet<string> {
            "47808d23c67033d4bbab86a1070fd62f", "1c1ebf5370939a9418da93176cc44cd9", // Cure Light / Moderate
            "6e81a6679a0889a429dec9cedcf3729c", "0d657aa811b310e4bbd8586e60156a2d", // Cure Serious / Critical
            "f5fc9a1a2a3c1a946a31b320d1dd31b2",                                     // ChannelEnergy (heal)
        };

        static bool Heals(TacticsRule r) =>
            r.Action.Type == ActionType.Heal
            || HealGuids.Contains(r.Action.AbilityId)
            || r.Action.FallbackAbilityIds.Any(HealGuids.Contains);

        // Self-heal is the user's global rule; only a healer heals others.
        [Fact]
        public void Only_the_healer_pack_heals() {
            var presets = DefaultPresets.Build().ToDictionary(p => p.Id);
            foreach (var pack in DefaultPacks.Build()) {
                if (pack.Id == "default-pack-healer") continue;
                foreach (var id in pack.PresetIds)
                    Assert.False(Heals(presets[id]), $"{pack.Name}: {id} heals");
            }
        }

        [Fact]
        public void Healer_pack_heals_others() {
            var presets = DefaultPresets.Build().ToDictionary(p => p.Id);
            var healer = DefaultPacks.Build().Single(p => p.Id == "default-pack-healer");
            Assert.Contains(healer.PresetIds, id => Heals(presets[id]) && presets[id].Target.Type != TargetType.Self);
        }

        // The game AI already attacks *something*; an attack rule earns its place only
        // by choosing the target (the "Attack Nearest" complaint).
        [Fact]
        public void Pack_attack_rules_always_pick_a_target_by_condition() {
            var presets = DefaultPresets.Build().ToDictionary(p => p.Id);
            foreach (var pack in DefaultPacks.Build())
                foreach (var id in pack.PresetIds) {
                    var r = presets[id];
                    if (r.Action.Type != ActionType.AttackTarget) continue;
                    Assert.True(r.ConditionGroups.Count > 0, $"{pack.Name}: {id} attacks unconditionally");
                }
        }

        // The ally bucket includes downed and dead companions; a buff aimed at the
        // condition-matched ally must exclude them or the cast is wasted on a corpse.
        [Fact]
        public void Ally_targeted_rules_require_a_living_ally() {
            foreach (var r in DefaultPresets.Build()) {
                if (r.Target.Type != TargetType.ConditionTarget) continue;
                foreach (var g in r.ConditionGroups) {
                    if (!g.Conditions.Any(c => c.Subject == ConditionSubject.Ally)) continue;
                    Assert.True(g.Conditions.Any(c => c.Subject == ConditionSubject.Ally
                            && c.Property == ConditionProperty.HpPercent
                            && c.Operator == ConditionOperator.GreaterThan && c.Value == "0"),
                        $"{r.Id}: ally group without HpPercent > 0");
                }
            }
        }

        // Fireball / Lightning Bolt cannot see our own melee in the blast: opt-in only.
        [Fact]
        public void Friendly_fire_aoe_ships_disabled() {
            var aoe = new[] { "2d81362af43aeac4387a3d4fced489c3", "d2cff9243a7ee804cb6d5be47af30c73" };
            foreach (var r in DefaultPresets.Build())
                if (aoe.Contains(r.Action.AbilityId) || r.Action.FallbackAbilityIds.Any(aoe.Contains))
                    Assert.False(r.Enabled, $"{r.Id} ships enabled");
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

        // A failed write (disk full, permissions) must stay unmarked so the next load retries it.
        [Fact]
        public void Failed_write_is_retried_next_load() {
            var seeded = new HashSet<string>();
            var write = DefaultSeeding.Plan(new[] { "a", "b" }, seeded, id => false);
            int written = DefaultSeeding.Write(write, seeded, id => id == "a");
            Assert.Equal(1, written);
            Assert.Contains("a", seeded);
            Assert.DoesNotContain("b", seeded);
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
