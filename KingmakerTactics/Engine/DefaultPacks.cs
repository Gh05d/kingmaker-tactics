using System.Collections.Generic;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Role packs seeded once per install (PackRegistry.SeedDefaults), so a new player can
    /// "Apply Pack" a sensible rule set per companion. Members reference DefaultPresets ids;
    /// rules a unit cannot perform (no rage, no Bless) are skipped by the validator, so a pack
    /// that only roughly fits a character is harmless. Names stay English (see DefaultPresets).
    /// </summary>
    public static class DefaultPacks {
        public static List<TacticsPack> Build() {
            return new List<TacticsPack> {
                Pack("default-pack-frontline", "Frontline (Valerie, Amiri, Regongar)", 0,
                    "default-emergency-self-heal", "role-rage-on", "default-smite-evil", "role-attack-highest-threat"),
                Pack("default-pack-healer", "Healer (Harrim, Tristian)", 1,
                    "role-heal-ally-below-50", "default-party-channel-heal", "role-bless-if-missing", "role-attack-nearest"),
                Pack("default-pack-bard", "Bard (Linzi)", 2,
                    "role-inspire-courage-on", "role-heal-ally-below-30", "role-attack-nearest"),
                Pack("default-pack-arcane", "Arcane Caster (Octavia, Kalikke, Kanerah)", 3,
                    "default-emergency-self-heal", "role-mage-armor-if-missing", "role-damage-spell"),
                Pack("default-pack-archer", "Archer (Ekundayo)", 4,
                    "role-rapid-shot-on", "role-deadly-aim-on", "role-attack-nearest"),
                Pack("default-pack-skirmisher", "Skirmisher (Nok-Nok, Jaethal)", 5,
                    "default-coup-de-grace", "role-attack-lowest-hp"),
            };
        }

        static TacticsPack Pack(string id, string name, int color, params string[] presetIds) => new TacticsPack {
            Id = id,
            Name = name,
            ColorIndex = color,
            PresetIds = new List<string>(presetIds),
        };
    }
}
