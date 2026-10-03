using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic;

namespace KingmakerTactics.Compatibility {
    // Pure member mappings Wrath -> Kingmaker. Kingmaker keeps these as public fields on
    // UnitDescriptor (KM IL: UnitDescriptor::State/Resources/Progression are initonly fields,
    // Spellbooks a property); Wrath exposes them on UnitEntityData. No logic here:
    // behavioral differences belong at the call site with an IL citation.
    internal static class KingmakerShim {
        public static UnitState State(this UnitEntityData u) => u.Descriptor.State;
        public static IEnumerable<Spellbook> Spellbooks(this UnitEntityData u) => u.Descriptor.Spellbooks;
        public static UnitAbilityResourceCollection Resources(this UnitEntityData u) => u.Descriptor.Resources;
        public static UnitProgressionData Progression(this UnitEntityData u) => u.Descriptor.Progression;

        // Wrath's UnitCommands.ContainsOrQueued. Kingmaker: Contains(c) is m_Commands[c.Type] == c,
        // the queue is a public LinkedList (engine-verification.md §10).
        public static bool ContainsOrQueued(this UnitCommands commands, UnitCommand c) =>
            commands.Contains(c) || (commands.Queue != null && commands.Queue.Contains(c));

        // Wrath's Player.PartyAndPets. Kingmaker has at most one pet per unit (UnitDescriptor.Pet);
        // Player.AddCharacterToLists never puts a pet into m_Party (IL), so Party is pet-free;
        // the duplicate skip is defensive only.
        public static List<UnitEntityData> PartyAndPets(this Player p) =>
            MergePartyAndPets(p.Party, u => u.Descriptor.Pet);

        internal static List<T> MergePartyAndPets<T>(IEnumerable<T> party, Func<T, T> petOf) where T : class {
            var result = new List<T>();
            var seen = new HashSet<T>();
            foreach (var m in party) {
                if (m == null || !seen.Add(m)) continue;
                result.Add(m);
                var pet = petOf(m);
                if (pet != null && seen.Add(pet)) result.Add(pet);
            }
            return result;
        }
    }
}
