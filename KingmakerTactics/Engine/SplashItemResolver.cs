using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerTactics.Logging;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Finds a throwable splash item. Kingmaker IL: ItemEntity.IsUsableFromInventory is
    /// "!Player.IsInCombat" for every usable item, and TryUseFromInventory refuses otherwise —
    /// so in combat only flasks in a quick slot (owner.Abilities entry with SourceItem) can be
    /// thrown, exactly like for the player. Inventory flasks remain an out-of-combat source.
    /// </summary>
    public static class SplashItemResolver {
        public struct Pick {
            /// <summary>Quick-slot ability (issued as an animated cast); null for inventory picks.</summary>
            public AbilityData QuickSlot;
            /// <summary>Inventory stack (TryUseFromInventory); null for quick-slot picks.</summary>
            public ItemEntity Item;
            public BlueprintAbility ThrowAbility;
            public string Name;
        }

        public static Pick? FindBest(UnitEntityData owner, ThrowSplashMode mode) {
            var keys = new List<(string guid, bool usableNow)>();
            var picks = new List<Pick>();

            // Quick slots first: the only source the engine accepts in combat.
            foreach (var ability in owner.Abilities) {
                var source = ability.Data.SourceItem;
                if (source == null) continue;
                keys.Add((source.Blueprint.AssetGuid.ToString(), ability.Data.IsAvailable));
                picks.Add(new Pick { QuickSlot = ability.Data, ThrowAbility = ability.Blueprint, Name = source.Blueprint.name });
            }

            var inventory = Game.Instance?.Player?.Inventory;
            if (inventory != null) {
                foreach (var item in inventory) {
                    if (item == null || item.Count <= 0) continue;
                    var usable = item.Blueprint as BlueprintItemEquipmentUsable;
                    if (usable == null || usable.Ability == null) continue;
                    keys.Add((item.Blueprint.AssetGuid.ToString(), item.IsUsableFromInventory));
                    picks.Add(new Pick { Item = item, ThrowAbility = usable.Ability, Name = item.Blueprint.name });
                }
            }

            var index = ChooseIndex(keys, mode);
            if (index == null) return null;
            var pick = picks[index.Value];
            Log.Engine.Trace($"Splash pick for {owner.CharacterName}: {pick.Name} ({(pick.QuickSlot != null ? "quick slot" : "inventory")})");
            return pick;
        }

        /// <summary>Pure choice over (blueprint guid, usable now) candidates: non-splash and
        /// unusable entries never win; Any keeps list order, Strongest/Cheapest rank by registry.</summary>
        public static int? ChooseIndex(IList<(string guid, bool usableNow)> candidates, ThrowSplashMode mode) {
            int? best = null;
            int bestPrio = int.MinValue;
            for (int i = 0; i < candidates.Count; i++) {
                var (guid, usableNow) = candidates[i];
                if (!usableNow || !SplashItemRegistry.IsSplashItem(guid)) continue;
                int prio;
                switch (mode) {
                    case ThrowSplashMode.Strongest: prio = SplashItemRegistry.GetDamagePriority(guid); break;
                    case ThrowSplashMode.Cheapest: prio = -SplashItemRegistry.GetCostPriority(guid); break;
                    default: return i;
                }
                if (prio > bestPrio) { best = i; bestPrio = prio; }
            }
            return best;
        }
    }
}
