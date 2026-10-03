using System.Collections.Generic;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Seeded once per fresh install via PresetRegistry.SeedDefaults. Uses fixed IDs so
    /// "file exists?" check is idempotent across mod reloads and version bumps. User
    /// deletions and manual edits are never overwritten.
    ///
    /// Names stay English: SeedDefaults runs from Main.Load (UMM init), but
    /// LocalizationManager.CurrentLocale only becomes safe after game settings load —
    /// any i18n() here NREs at startup. Users can rename via the Presets tab.
    /// </summary>
    public static class DefaultPresets {
        public static List<TacticsRule> Build() {
            return new List<TacticsRule> {
                EmergencySelfHeal(),
                PartyChannelHeal(),
                CounterSwarms(),
                CoupDeGrace(),
                ChannelVsUndead(),
                SmiteEvil(),
                // Role building blocks for DefaultPacks (GUIDs verified against the Kingmaker
                // blueprint index, kingmaker/il-dump/blueprints-index.tsv).
                Attack("role-attack-highest-threat", "Attack Highest Threat", TargetType.EnemyHighestThreat),
                Attack("role-attack-nearest", "Attack Nearest", TargetType.EnemyNearest),
                Attack("role-attack-lowest-hp", "Attack Weakest (lowest HP)", TargetType.EnemyLowestHp),
                ToggleOn("role-rage-on", "Rage On", "df6a2cce8e3a9bd4592fb1968b83f730"),                    // StandartRageActivateableAbility
                ToggleOn("role-inspire-courage-on", "Inspire Courage On", "5250fe10c377fdb49be449dfe050ba70"), // InspireCourageToggleAbility
                ToggleOn("role-rapid-shot-on", "Rapid Shot On", "90a77bfe25ec2e14caf8bd5cde9febf2"),         // RapidShotToggleAbility
                ToggleOn("role-deadly-aim-on", "Deadly Aim On", "ccde5ab6edb84f346a74c17ea3e3a70c"),         // DeadlyAimToggleAbility
                HealAlly("role-heal-ally-below-50", "Heal Ally below 50%", "50"),
                HealAlly("role-heal-ally-below-30", "Heal Ally below 30%", "30"),
                SelfBuffIfMissing("role-bless-if-missing", "Bless (if missing)",
                    "90e59f4a4ada87243b7b3535a06d0638", "87b8c6270ea85c743afc734dfe99afee"),            // Bless / BlessBuff
                SelfBuffIfMissing("role-mage-armor-if-missing", "Mage Armor (if missing)",
                    "9e1ad5d6f87d19e4d8883d63a6e35568", "a92acdf18049d784eaa8f2004f5d2304"),            // MageArmor / MageArmorBuff
                DamageSpell(),
            };
        }

        static TacticsRule Attack(string id, string name, TargetType target) => new TacticsRule {
            Id = id,
            Name = name,
            Enabled = true,
            CooldownRounds = 0,
            ConditionGroups = new List<ConditionGroup>(),
            Action = new ActionDef { Type = ActionType.AttackTarget },
            Target = new TargetDef { Type = target },
        };

        static TacticsRule ToggleOn(string id, string name, string activatableGuid) => new TacticsRule {
            Id = id,
            Name = name,
            Enabled = true,
            CooldownRounds = 0,
            ConditionGroups = new List<ConditionGroup>(),
            Action = new ActionDef { Type = ActionType.ToggleActivatable, AbilityId = activatableGuid, ToggleMode = ToggleMode.On },
            Target = new TargetDef { Type = TargetType.Self },
        };

        static TacticsRule HealAlly(string id, string name, string belowPercent) => new TacticsRule {
            Id = id,
            Name = name,
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Ally,
                        Property = ConditionProperty.HpPercent,
                        Operator = ConditionOperator.LessThan,
                        Value = belowPercent,
                    },
                }},
            },
            Action = new ActionDef { Type = ActionType.Heal, HealMode = HealMode.Any },
            Target = new TargetDef { Type = TargetType.AllyLowestHp },
        };

        static TacticsRule SelfBuffIfMissing(string id, string name, string spellGuid, string buffGuid) => new TacticsRule {
            Id = id,
            Name = name,
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Self,
                        Property = ConditionProperty.HasBuff,
                        Operator = ConditionOperator.NotEqual,
                        Value = buffGuid,
                    },
                }},
            },
            Action = new ActionDef { Type = ActionType.CastSpell, AbilityId = spellGuid },
            Target = new TargetDef { Type = TargetType.Self },
        };

        // Single-target only: an AoE at the highest-threat enemy usually catches our own melee.
        static TacticsRule DamageSpell() => new TacticsRule {
            Id = "role-damage-spell",
            Name = "Damage Spell (Scorching Ray > Magic Missile > Ray of Frost > Acid Splash)",
            Enabled = true,
            CooldownRounds = 0,
            ConditionGroups = new List<ConditionGroup>(),
            Action = new ActionDef {
                Type = ActionType.CastSpell,
                AbilityId = "cdb106d53c65bbc4086183d54c3b97c7",                  // ScorchingRay
                FallbackAbilityIds = new List<string> {
                    "4ac47ddb9fa1eaf43a1b6809980cfbd2",                          // MagicMissile
                    "9af2ab69df6538f4793b2f9c3cc85603",                          // RayOfFrost
                    "0c852a2405dd9f14a8bbcfaf245ff823",                          // AcidSplash
                },
            },
            Target = new TargetDef { Type = TargetType.EnemyHighestThreat },
        };

        static TacticsRule EmergencySelfHeal() => new TacticsRule {
            Id = "default-emergency-self-heal",
            Name = "Emergency Self-Heal",
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Self,
                        Property = ConditionProperty.HpPercent,
                        Operator = ConditionOperator.LessThan,
                        Value = "30",
                    },
                }},
            },
            Action = new ActionDef { Type = ActionType.Heal, HealMode = HealMode.Any },
            Target = new TargetDef { Type = TargetType.Self },
        };

        static TacticsRule PartyChannelHeal() => new TacticsRule {
            Id = "default-party-channel-heal",
            Name = "Party Heal (Channel Positive)",
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.AllyCount,
                        Property = ConditionProperty.HpPercent,
                        Operator = ConditionOperator.LessThan,
                        Value = "60",
                        Value2 = "2",
                    },
                }},
            },
            Action = new ActionDef {
                Type = ActionType.CastAbility,
                AbilityId = "f5fc9a1a2a3c1a946a31b320d1dd31b2",  // Cleric ChannelEnergy (heal)
            },
            Target = new TargetDef { Type = TargetType.Self },
        };

        static TacticsRule CounterSwarms() => new TacticsRule {
            Id = "default-counter-swarms",
            Name = "Counter Swarms (Splash)",
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Enemy,
                        Property = ConditionProperty.CreatureType,
                        Operator = ConditionOperator.Equal,
                        Value = "Swarm",
                    },
                }},
            },
            Action = new ActionDef { Type = ActionType.ThrowSplash, SplashMode = ThrowSplashMode.Strongest },
            Target = new TargetDef { Type = TargetType.EnemyNearest },
        };

        static TacticsRule CoupDeGrace() => new TacticsRule {
            Id = "default-coup-de-grace",
            Name = "Coup de Grace on Helpless",
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Enemy,
                        Property = ConditionProperty.HasCondition,
                        Operator = ConditionOperator.Equal,
                        Value = "Sleeping",
                    },
                }},
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Enemy,
                        Property = ConditionProperty.HasCondition,
                        Operator = ConditionOperator.Equal,
                        Value = "Paralyzed",
                    },
                }},
            },
            Action = new ActionDef { Type = ActionType.AttackTarget },
            Target = new TargetDef { Type = TargetType.ConditionTarget },
        };

        static TacticsRule ChannelVsUndead() => new TacticsRule {
            Id = "default-channel-vs-undead",
            Name = "Channel Against Undead",
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.EnemyCount,
                        Property = ConditionProperty.CreatureType,
                        Operator = ConditionOperator.Equal,
                        Value = "Undead",
                        Value2 = "3",
                    },
                }},
            },
            Action = new ActionDef {
                Type = ActionType.CastAbility,
                AbilityId = "279447a6bf2d3544d93a0a39c3b8e91d",  // Cleric ChannelPositiveHarm
            },
            Target = new TargetDef { Type = TargetType.Self },
        };

        static TacticsRule SmiteEvil() => new TacticsRule {
            Id = "default-smite-evil",
            Name = "Smite Evil",
            Enabled = true,
            CooldownRounds = 1,
            ConditionGroups = new List<ConditionGroup> {
                new ConditionGroup { Conditions = new List<Condition> {
                    new Condition {
                        Subject = ConditionSubject.Enemy,
                        Property = ConditionProperty.Alignment,
                        Operator = ConditionOperator.Equal,
                        Value = "Evil",
                    },
                }},
            },
            Action = new ActionDef {
                Type = ActionType.CastAbility,
                AbilityId = "7bb9eb2042e67bf489ccd1374423cdec",  // Paladin SmiteEvilAbility
            },
            Target = new TargetDef { Type = TargetType.EnemyHighestThreat },
        };
    }
}
