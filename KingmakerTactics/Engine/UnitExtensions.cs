using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;

namespace KingmakerTactics.Engine {
    public static class UnitExtensions {
        // HD matches the game's own ContextConditionHitDice check (IL-verified in Wrath):
        // reads UnitProgressionData.CharacterLevel. Racial HD is already folded
        // into CharacterLevel for monsters (HD-gated spells: Sleep, Color Spray, Hold Person).
        public static int GetHD(UnitEntityData unit) {
            var progression = unit?.Descriptor?.Progression;
            return progression?.CharacterLevel ?? 0;
        }

        // Effective HD for margin-vs-party-level comparisons (EnemyHDMinusPartyLevel).
        // Kingmaker has no mythic levels, so this equals GetHD(); kept as a separate
        // helper so call sites stay aligned with Wrath Tactics.
        public static int GetEffectiveHD(UnitEntityData unit) {
            var p = unit?.Descriptor?.Progression;
            if (p == null) return 0;
            return p.CharacterLevel;
        }

        // Looks up the target's modified save for the given save type. Returns 0
        // for SavingThrowType.Unknown — callers must pre-check the enum so "no
        // save" doesn't silently compare against 0.
        public static int GetSave(UnitEntityData unit, SavingThrowType type) {
            if (unit == null) return 0;
            switch (type) {
                case SavingThrowType.Fortitude: return unit.Stats.SaveFortitude.ModifiedValue;
                case SavingThrowType.Reflex:    return unit.Stats.SaveReflex.ModifiedValue;
                case SavingThrowType.Will:      return unit.Stats.SaveWill.ModifiedValue;
                default:                        return 0;
            }
        }

        // Matches a subject unit against a HasClass condition value.
        // Value format: "group:<spellcaster|arcane|divine|martial>" or "class:<InternalName>".
        // Groups resolve via blueprint flags (IsArcaneCaster/IsDivineCaster) or
        // the presence of any Spellbook. Specific classes match the unit's Progression.Classes
        // list against the stripped blueprint name.
        public static bool MatchesClassValue(UnitEntityData unit, string value) {
            if (string.IsNullOrEmpty(value)) return false;
            var classes = unit?.Descriptor?.Progression?.Classes;
            if (classes == null || classes.Count == 0) return false;

            if (value.StartsWith("group:")) {
                var group = value.Substring(6);
                switch (group) {
                    case "spellcaster":
                        return unit.Spellbooks != null && unit.Spellbooks.Any();
                    case "arcane":
                        return classes.Any(c => c?.CharacterClass != null && c.CharacterClass.IsArcaneCaster);
                    case "divine":
                        return classes.Any(c => c?.CharacterClass != null && c.CharacterClass.IsDivineCaster);
                    case "martial":
                        return classes.Any(c => c?.CharacterClass != null
                            && !c.CharacterClass.IsArcaneCaster
                            && !c.CharacterClass.IsDivineCaster);
                    default:
                        return false;
                }
            }
            if (value.StartsWith("class:")) {
                var stripped = value.Substring(6);
                return classes.Any(c =>
                    c?.CharacterClass != null
                    && ClassProvider.StripSuffix(c.CharacterClass.name) == stripped);
            }
            return false;
        }
    }
}
