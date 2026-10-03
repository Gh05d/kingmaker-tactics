using System.Collections.Generic;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Building blocks for DefaultPacks: one game plan per role, ordered by priority.
    /// Rules that only re-state what the game AI does anyway (plain "attack nearest") are
    /// out — an attack rule must pick its target by condition. Healing lives only in the
    /// healer pack; self-heal is left to the player's global rules.
    ///
    /// GUIDs verified against the Kingmaker blueprint index (kingmaker/il-dump/blueprints-index.tsv).
    /// Condition semantics used here (ConditionEvaluator): Enemy-scope conditions of one group
    /// must hold for the SAME enemy (a Pick subject sorts the search), EnemyCount counts the
    /// enemies passing the group's other enemy conditions, the Ally bucket excludes the owner
    /// but includes downed/dead companions (hence "Ally HpPercent > 0" on every ally buff).
    /// </summary>
    public static class DefaultRolePresets {
        // Spells / abilities
        const string Haste = "486eaff58293f6441a5c2759c4872f98";
        const string HoldPerson = "c7104f7526c4c524f91474614054547e";
        const string HideousLaughter = "fd4d9fd7f87575d47aafe2a64a6e2d8d";
        const string Slow = "f492622e473d34747806bdb39356eb89";
        const string Heroism = "5ab0d42fb68c9e34abae4921822b9d63";
        const string Prayer = "faabd2cc67efa4646ac58c7bb3e40fcc";
        const string Bless = "90e59f4a4ada87243b7b3535a06d0638";
        const string ShieldOfFaith = "183d5bb91dea3a1489a6db6c9cb64445";
        const string RemoveFear = "55a037e514c0ee14a8e3ed14b47061de";
        const string RemoveParalysis = "f8bce986adfc88544a42bf4ab7ae75b2";
        const string RestorationLesser = "e84fc922ccf952943b5240293669b171";
        const string MageArmor = "9e1ad5d6f87d19e4d8883d63a6e35568";
        const string MirrorImage = "3e4ab69ada402d145a5e0ad3ad4b8564";
        const string Fireball = "2d81362af43aeac4387a3d4fced489c3";
        const string LightningBolt = "d2cff9243a7ee804cb6d5be47af30c73";
        const string SmiteEvil = "7bb9eb2042e67bf489ccd1374423cdec";        // Paladin SmiteEvilAbility
        // Activatables
        const string Rage = "df6a2cce8e3a9bd4592fb1968b83f730";             // StandartRageActivateableAbility
        const string InspireCourage = "5250fe10c377fdb49be449dfe050ba70";   // InspireCourageToggleAbility
        const string PowerAttack = "a7b339e4f6ff93a4697df5d7a87ff619";      // PowerAttackToggleAbility
        const string RapidShot = "90a77bfe25ec2e14caf8bd5cde9febf2";        // RapidShotToggleAbility
        const string DeadlyAim = "ccde5ab6edb84f346a74c17ea3e3a70c";        // DeadlyAimToggleAbility
        // Buffs
        const string HasteBuffA = "8d20b0a6129bd814eb0146041879f38a";       // "Haste"
        const string HasteBuffB = "03464790f40c3c24aa684b57155f3280";       // "HasteBuff"
        const string HeroismBuff = "87ab2fed7feaaff47b62a3320a57ad8d";
        const string PrayerBuff = "789bae3802e7b6b4c8097aaf566a1cf5";
        const string BlessBuff = "87b8c6270ea85c743afc734dfe99afee";
        const string ShieldOfFaithBuff = "5274ddc289f4a7447b7ace68ad8bebb0";
        const string MageArmorBuff = "a92acdf18049d784eaa8f2004f5d2304";
        const string MirrorImageBuff = "98dc7e7cc6ef59f4abe20c65708ac623";
        const string HideousLaughterBuff = "4b1f07a71a982824988d7f48cd49f3f8";

        public static List<TacticsRule> Build() {
            return new List<TacticsRule> {
                // --- Openers (first two rounds) and toggles ---
                Rule("role-haste-opener", "Opener: Haste (3+ enemies)", 1,
                    Cast(Haste), Target(TargetType.Self),
                    Group(FirstRounds(), SelfMissing(HasteBuffA), SelfMissing(HasteBuffB), EnemiesAtLeast(3))),
                Rule("role-prayer-opener", "Opener: Prayer (3+ enemies)", 1,
                    Cast(Prayer), Target(TargetType.Self),
                    Group(FirstRounds(), SelfMissing(PrayerBuff), EnemiesAtLeast(3))),
                Rule("role-bless-opener", "Opener: Bless (if no Prayer)", 1,
                    Cast(Bless), Target(TargetType.Self),
                    Group(FirstRounds(), SelfMissing(BlessBuff), SelfMissing(PrayerBuff))),
                Rule("role-mage-armor-if-missing", "Mage Armor (if missing)", 1,
                    Cast(MageArmor), Target(TargetType.Self),
                    Group(SelfMissing(MageArmorBuff))),
                Rule("role-inspire-courage-on", "Inspire Courage On", 0,
                    Toggle(InspireCourage), Target(TargetType.Self)),
                // Rage rounds are limited: not for a single weak mob.
                Rule("role-rage-on", "Rage On (2+ enemies or a strong one)", 0,
                    Toggle(Rage), Target(TargetType.Self),
                    Group(EnemiesAtLeast(2)),
                    Group(C(ConditionSubject.Enemy, ConditionProperty.EnemyHDMinusPartyLevel, ConditionOperator.GreaterOrEqual, "0"))),
                Rule("role-power-attack-on", "Power Attack On", 0,
                    Toggle(PowerAttack), Target(TargetType.Self)),
                Rule("role-rapid-shot-on", "Rapid Shot On (bow in hand)", 0,
                    Toggle(RapidShot), Target(TargetType.Self),
                    Group(C(ConditionSubject.Self, ConditionProperty.WieldsRangedWeapon, ConditionOperator.Equal, "true"))),
                Rule("role-deadly-aim-on", "Deadly Aim On (bow in hand)", 0,
                    Toggle(DeadlyAim), Target(TargetType.Self),
                    Group(C(ConditionSubject.Self, ConditionProperty.WieldsRangedWeapon, ConditionOperator.Equal, "true"))),

                // --- Healer ---
                Rule("role-heal-ally-emergency", "Heal Ally below 35% (strongest)", 1,
                    new ActionDef { Type = ActionType.Heal, HealMode = HealMode.Strongest }, Target(TargetType.AllyLowestHp),
                    Group(C(ConditionSubject.Ally, ConditionProperty.HpPercent, ConditionOperator.LessThan, "35"))),
                Rule("role-heal-ally-top-up", "Heal Ally below 60% (weakest)", 1,
                    new ActionDef { Type = ActionType.Heal, HealMode = HealMode.Weakest }, Target(TargetType.AllyLowestHp),
                    Group(C(ConditionSubject.Ally, ConditionProperty.HpPercent, ConditionOperator.LessThan, "60"))),
                Rule("role-remove-paralysis", "Remove Paralysis (paralyzed or slowed ally)", 1,
                    Cast(RemoveParalysis), Target(TargetType.ConditionTarget),
                    Group(AllyAlive(), C(ConditionSubject.Ally, ConditionProperty.HasCondition, ConditionOperator.Equal, "Paralyzed")),
                    Group(AllyAlive(), C(ConditionSubject.Ally, ConditionProperty.HasCondition, ConditionOperator.Equal, "Slowed"))),
                Rule("role-remove-fear", "Remove Fear (frightened or shaken ally)", 1,
                    Cast(RemoveFear), Target(TargetType.ConditionTarget),
                    Group(AllyAlive(), C(ConditionSubject.Ally, ConditionProperty.HasCondition, ConditionOperator.Equal, "Frightened")),
                    Group(AllyAlive(), C(ConditionSubject.Ally, ConditionProperty.HasCondition, ConditionOperator.Equal, "Shaken"))),
                Rule("role-lesser-restoration", "Lesser Restoration (ability damage)", 2,
                    Cast(RestorationLesser), Target(TargetType.ConditionTarget),
                    Group(AllyAlive(), C(ConditionSubject.Ally, ConditionProperty.AbilityDamage, ConditionOperator.Equal, "true"))),
                Rule("role-shield-of-faith-ally", "Shield of Faith on attacked ally", 2,
                    Cast(ShieldOfFaith), Target(TargetType.ConditionTarget),
                    Group(AllyAlive(), AllyAttacked(), AllyMissing(ShieldOfFaithBuff))),

                // --- Bard / arcane control and support ---
                Rule("role-heroism-ally", "Heroism on attacked ally", 2,
                    Cast(Heroism), Target(TargetType.ConditionTarget),
                    Group(AllyAlive(), AllyAttacked(), AllyMissing(HeroismBuff))),
                Rule("role-hold-person", "Hold Person (humanoid, weakest Will)", 2,
                    Cast(HoldPerson), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyLowestWill, ConditionProperty.CreatureType, ConditionOperator.Equal, "Humanoid"),
                        C(ConditionSubject.Enemy, ConditionProperty.HpPercent, ConditionOperator.GreaterThan, "50"),
                        C(ConditionSubject.Enemy, ConditionProperty.HasCondition, ConditionOperator.NotEqual, "Paralyzed"))),
                Rule("role-hideous-laughter", "Hideous Laughter (weakest Will)", 2,
                    Cast(HideousLaughter), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyLowestWill, ConditionProperty.HpPercent, ConditionOperator.GreaterThan, "50"),
                        C(ConditionSubject.Enemy, ConditionProperty.HasCondition, ConditionOperator.NotEqual, "Paralyzed"),
                        C(ConditionSubject.Enemy, ConditionProperty.HasBuff, ConditionOperator.NotEqual, HideousLaughterBuff))),
                Rule("role-slow-group", "Slow the densest group (3+ unslowed enemies)", 3,
                    Cast(Slow), Target(TargetType.EnemyMostEnemyNeighbors),
                    Group(Count(ConditionSubject.EnemyCount, ConditionProperty.HasCondition, ConditionOperator.NotEqual, "Slowed", 3))),
                Rule("role-mirror-image-when-attacked", "Mirror Image when attacked", 2,
                    Cast(MirrorImage), Target(TargetType.Self),
                    Group(SelfMissing(MirrorImageBuff),
                        C(ConditionSubject.Enemy, ConditionProperty.IsTargetingSelf, ConditionOperator.Equal, "true"))),
                // Cannot see our own melee in the blast radius: shipped disabled, opt in per fight style.
                Rule("role-fireball-group", "Fireball > Lightning Bolt on 3+ grouped enemies (CHECK FRIENDLY FIRE)", 2,
                    Cast(Fireball, LightningBolt), Target(TargetType.EnemyMostEnemyNeighbors),
                    enabled: false,
                    groups: new[] { Group(EnemiesAtLeast(3)) }),
                Rule("role-damage-spell", "Damage Spell (Scorching Ray > Magic Missile > Ray of Frost > Acid Splash)", 0,
                    Cast("cdb106d53c65bbc4086183d54c3b97c7",                     // ScorchingRay
                        "4ac47ddb9fa1eaf43a1b6809980cfbd2",                      // MagicMissile
                        "9af2ab69df6538f4793b2f9c3cc85603",                      // RayOfFrost
                        "0c852a2405dd9f14a8bbcfaf245ff823"),                     // AcidSplash
                    Target(TargetType.EnemyHighestThreat)),

                // --- Frontline ---
                Rule("role-smite-strong-evil", "Smite Evil (strong evil enemy)", 3,
                    new ActionDef { Type = ActionType.CastAbility, AbilityId = SmiteEvil }, Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyBiggestThreat, ConditionProperty.Alignment, ConditionOperator.Equal, "Evil"),
                        C(ConditionSubject.Enemy, ConditionProperty.EnemyHDMinusPartyLevel, ConditionOperator.GreaterOrEqual, "-2"))),
                Rule("role-finish-adjacent", "Finish adjacent enemy below 25%", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyLowestHp, ConditionProperty.WithinRange, ConditionOperator.LessOrEqual, "Melee"),
                        C(ConditionSubject.Enemy, ConditionProperty.HpPercent, ConditionOperator.LessThan, "25"))),
                Rule("role-protect-allies", "Intercept enemy attacking an ally (within 10 m)", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyNearest, ConditionProperty.IsTargetingAlly, ConditionOperator.Equal, "true"),
                        C(ConditionSubject.Enemy, ConditionProperty.WithinRange, ConditionOperator.LessOrEqual, "Short"))),

                // --- Archer ---
                Rule("role-shoot-weakest", "Shoot weakest enemy below 30%", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(C(ConditionSubject.EnemyLowestHp, ConditionProperty.HpPercent, ConditionOperator.LessThan, "30"))),
                Rule("role-shoot-enemy-archers", "Shoot enemy archers", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(C(ConditionSubject.EnemyLowestHp, ConditionProperty.WieldsRangedWeapon, ConditionOperator.Equal, "true"))),
                Rule("role-shoot-attackers-of-allies", "Shoot enemy attacking an ally", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(C(ConditionSubject.EnemyLowestHp, ConditionProperty.IsTargetingAlly, ConditionOperator.Equal, "true"))),

                // --- Skirmisher (sneak attack) ---
                Rule("role-attack-flanked", "Attack flanked enemy (within 10 m)", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyNearest, ConditionProperty.IsFlanked, ConditionOperator.Equal, "true"),
                        C(ConditionSubject.Enemy, ConditionProperty.WithinRange, ConditionOperator.LessOrEqual, "Short"))),
                Rule("role-finish-nearby", "Finish enemy below 30% (within 10 m)", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyLowestHp, ConditionProperty.HpPercent, ConditionOperator.LessThan, "30"),
                        C(ConditionSubject.Enemy, ConditionProperty.WithinRange, ConditionOperator.LessOrEqual, "Short"))),
                Rule("role-flank-allys-target", "Flank the enemy an ally fights (within 10 m)", 0,
                    Attack(), Target(TargetType.ConditionTarget),
                    Group(
                        C(ConditionSubject.EnemyNearest, ConditionProperty.IsTargetedByAlly, ConditionOperator.Equal, "true"),
                        C(ConditionSubject.Enemy, ConditionProperty.WithinRange, ConditionOperator.LessOrEqual, "Short"))),
            };
        }

        static TacticsRule Rule(string id, string name, int cooldown, ActionDef action, TargetDef target,
            params ConditionGroup[] groups) => Rule(id, name, cooldown, action, target, true, groups);

        static TacticsRule Rule(string id, string name, int cooldown, ActionDef action, TargetDef target,
            bool enabled, ConditionGroup[] groups) => new TacticsRule {
            Id = id,
            Name = name,
            Enabled = enabled,
            CooldownRounds = cooldown,
            ConditionGroups = new List<ConditionGroup>(groups),
            Action = action,
            Target = target,
        };

        static ActionDef Cast(string guid, params string[] fallbacks) =>
            new ActionDef { Type = ActionType.CastSpell, AbilityId = guid, FallbackAbilityIds = new List<string>(fallbacks) };

        static ActionDef Toggle(string guid) =>
            new ActionDef { Type = ActionType.ToggleActivatable, AbilityId = guid, ToggleMode = ToggleMode.On };

        static ActionDef Attack() => new ActionDef { Type = ActionType.AttackTarget };

        static TargetDef Target(TargetType type) => new TargetDef { Type = type };

        static ConditionGroup Group(params Condition[] conditions) =>
            new ConditionGroup { Conditions = new List<Condition>(conditions) };

        static Condition C(ConditionSubject subject, ConditionProperty property, ConditionOperator op, string value) =>
            new Condition { Subject = subject, Property = property, Operator = op, Value = value };

        static Condition Count(ConditionSubject subject, ConditionProperty property, ConditionOperator op, string value, int atLeast) =>
            new Condition {
                Subject = subject, Property = property, Operator = op, Value = value,
                CountOperator = ConditionOperator.GreaterOrEqual, Value2 = atLeast.ToString(),
            };

        // CombatRounds is fractional ((now - start) / 6): "< 2" = the first 12 s.
        static Condition FirstRounds() => C(ConditionSubject.Combat, ConditionProperty.CombatRounds, ConditionOperator.LessThan, "2");

        static Condition EnemiesAtLeast(int n) =>
            Count(ConditionSubject.EnemyCount, ConditionProperty.HpPercent, ConditionOperator.GreaterThan, "0", n);

        static Condition SelfMissing(string buff) => C(ConditionSubject.Self, ConditionProperty.HasBuff, ConditionOperator.NotEqual, buff);

        static Condition AllyMissing(string buff) => C(ConditionSubject.Ally, ConditionProperty.HasBuff, ConditionOperator.NotEqual, buff);

        static Condition AllyAlive() => C(ConditionSubject.Ally, ConditionProperty.HpPercent, ConditionOperator.GreaterThan, "0");

        static Condition AllyAttacked() => C(ConditionSubject.Ally, ConditionProperty.IsTargetedByEnemy, ConditionOperator.Equal, "true");
    }
}
