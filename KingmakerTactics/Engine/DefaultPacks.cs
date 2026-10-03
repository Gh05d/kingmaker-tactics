using System.Collections.Generic;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Role packs seeded once per install (PackRegistry.SeedDefaults), so a new player can
    /// "Apply Pack" a sensible rule set per companion. Members reference DefaultPresets ids,
    /// listed in priority order. Rules a unit cannot perform (no rage, no Haste slot) are
    /// skipped by the validator, so a pack that only roughly fits a character is harmless.
    /// Only the healer pack heals; self-heal belongs in the player's global rules.
    /// Names stay English (see DefaultPresets).
    /// </summary>
    public static class DefaultPacks {
        public static List<TacticsPack> Build() {
            return new List<TacticsPack> {
                Pack("default-pack-frontline", "Frontline (Valerie, Amiri, Regongar)", 0,
                    "role-rage-on", "role-power-attack-on", "role-smite-strong-evil",
                    "role-finish-adjacent", "role-protect-allies"),
                Pack("default-pack-healer", "Healer (Harrim, Tristian)", 1,
                    "role-heal-ally-emergency", "default-party-channel-heal", "role-remove-paralysis",
                    "role-heal-ally-top-up", "role-remove-fear", "role-lesser-restoration",
                    "role-prayer-opener", "role-bless-opener", "role-shield-of-faith-ally"),
                Pack("default-pack-bard", "Bard (Linzi)", 2,
                    "role-inspire-courage-on", "role-haste-opener", "role-hold-person",
                    "role-hideous-laughter", "role-slow-group", "role-heroism-ally"),
                Pack("default-pack-arcane", "Arcane Caster (Octavia, Kalikke, Kanerah)", 3,
                    "role-mage-armor-if-missing", "role-haste-opener", "role-mirror-image-when-attacked",
                    "role-hold-person", "role-slow-group", "role-fireball-group", "role-damage-spell"),
                Pack("default-pack-archer", "Archer (Ekundayo)", 4,
                    "role-rapid-shot-on", "role-deadly-aim-on", "role-shoot-weakest",
                    "role-shoot-enemy-archers", "role-shoot-attackers-of-allies"),
                Pack("default-pack-skirmisher", "Skirmisher (Nok-Nok, Jaethal)", 5,
                    "default-coup-de-grace", "role-attack-flanked", "role-finish-nearby",
                    "role-flank-allys-target"),
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
