using System.Collections.Generic;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerTactics.Engine {
    // Kingmaker has no AbilityData.GetConversions and AbilityData.Variants is always null
    // (engine-verification.md §13). This mirrors how Kingmaker's own action bar builds the
    // list (KM IL: MechanicActionBarSlotMemorizedSpell.GetConvertedAbilityData):
    // spontaneous conversions from the spellbook, then the blueprint's variants, each as
    // new AbilityData(bp, spellbook) { ConvertedFrom = parent } so SpellLevel and slot
    // lookups resolve through the parent. Restore-spell-slot abilities are left out: they
    // need a concrete memorized SpellSlot, which a tactics rule never has.
    internal static class AbilityConversions {
        public static List<AbilityData> GetConversions(this AbilityData parent) {
            var result = new List<AbilityData>();
            if (parent?.Blueprint == null) return result;
            var book = parent.Spellbook;
            if (book != null) {
                var spontaneous = book.GetSpontaneousConversionSpells(parent);
                if (spontaneous != null) {
                    foreach (var bp in spontaneous) {
                        if (bp == null) continue;
                        result.Add(new AbilityData(bp, book) { ConvertedFrom = parent });
                    }
                }
            }
            var variants = parent.Blueprint.Variants;
            if (variants != null) {
                foreach (var bp in variants) {
                    if (bp == null) continue;
                    // Copy ctor keeps the parent's metamagic and sets ConvertedFrom (KM IL: AbilityData::.ctor(AbilityData, BlueprintAbility)).
                    result.Add(book != null ? new AbilityData(bp, book) { ConvertedFrom = parent } : new AbilityData(parent, bp));
                }
            }
            return result;
        }
    }
}
