using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerTactics.Logging;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    public static class CommandExecutor {
        public static bool Execute(ActionDef action, UnitEntityData owner, ResolvedTarget target, out UnitCommand issuedCommand) {
            issuedCommand = null;
            try {
                switch (action.Type) {
                    case ActionType.CastSpell:
                    case ActionType.CastAbility:
                        return ExecuteCastSpell(action, owner, target, out issuedCommand);
                    case ActionType.UseItem:
                        return ExecuteUseItem(action.AbilityId, owner, target, out issuedCommand);
                    case ActionType.ToggleActivatable:
                        return ExecuteToggleActivatable(action.AbilityId, owner, action.ToggleMode);
                    case ActionType.AttackTarget:
                        return ExecuteAttack(owner, target.Unit, out issuedCommand);
                    case ActionType.Heal:
                        return ExecuteHeal(action, owner, target.Unit, out issuedCommand);
                    case ActionType.ThrowSplash:
                        return ExecuteThrowSplash(action, owner, target.Unit, out issuedCommand);
                    case ActionType.SwitchWeaponSet:
                        return ExecuteSwitchWeaponSet(action.WeaponSetIndex, owner, out issuedCommand);
                    case ActionType.MoveToTarget:
                        return ExecuteMoveToTarget(action.MoveWithin, owner, target, out issuedCommand);
                    case ActionType.DoNothing:
                        return true;
                    default:
                        return false;
                }
            } catch (Exception ex) {
                Log.Engine.Error(ex, $"Failed to execute {action.Type} for {owner.CharacterName}");
                return false;
            }
        }

        static TargetWrapper BuildTargetWrapper(ResolvedTarget target, UnitEntityData owner) {
            if (target.IsPoint) return new TargetWrapper(target.Point.Value);
            if (target.Unit != null) return new TargetWrapper(target.Unit);
            return new TargetWrapper(owner); // fallback preserves pre-refactor "no target = self" behavior
        }

        // UnitCommands.Run can silently discard the command instead of slotting it:
        // TryMergeInto folds a same-Ability UnitUseAbility (or a same-target UnitAttack)
        // into a still-running command, and the engine may refuse to slot it at all
        // (engine-verification.md §2 — Kingmaker has no CanRunCommand veto like Wrath).
        // A discarded command never starts and never finishes — tracking it would
        // wedge ActiveRuleTracker's priority gate until combat end (rules "skipped").
        // Returns the command actually in flight (the issued one, or the merged slot
        // occupant), or null when the engine vetoed the run entirely.
        static UnitCommand RunVerified(UnitEntityData owner, UnitCommand command) {
            owner.Commands.Run(command);
            var slots = owner.Commands.Raw;
            for (int i = 0; i < slots.Length; i++) {
                if (ReferenceEquals(slots[i], command)) {
                    PlayerCommandGuard.Track(owner, command);
                    return command;
                }
            }
            // Merge case: the cast IS happening via the re-promoted PreviousCommand —
            // treat as success but gate on the live occupant, not our dead object.
            if (command is UnitUseAbility ours) {
                for (int i = 0; i < slots.Length; i++) {
                    if (slots[i] is UnitUseAbility other && other.IsRunning && other.Spell == ours.Spell) {
                        PlayerCommandGuard.Track(owner, other);
                        Log.Engine.Debug($"Commands.Run merged {ours.Spell?.Name} into running command for {owner.CharacterName} — gating on slot occupant");
                        return other;
                    }
                }
            }
            // Kingmaker also merges a UnitAttack into a running attack on the same target
            // (engine-verification.md §2) — the attack IS happening; gate on the occupant.
            if (command is UnitAttack ourAttack) {
                for (int i = 0; i < slots.Length; i++) {
                    if (slots[i] is UnitAttack other && other.IsRunning && other.Target == ourAttack.Target) {
                        PlayerCommandGuard.Track(owner, other);
                        Log.Engine.Debug($"Commands.Run merged attack into running attack for {owner.CharacterName} — gating on slot occupant");
                        return other;
                    }
                }
            }
            // Queue case: UnitCommands.Run → TryAddToQueueInsteadOfRunImmediately parks the
            // command in Commands.Queue when the unit holds an uninterruptible running
            // command (a cast in progress) or is already running a command on the same
            // target we are close enough to reach (auto-attack on our cast target). The
            // engine flags the occupant InterruptAsSoonAsPossible and runs ours from the
            // queue once slot and paired slot are free — exactly what a player click does
            // in the same situation. Treat it as issued: track it, gate on it. Both
            // trackers drop it again if a later Run() clears the queue (Contains || Queue.Contains).
            var queue = owner.Commands.Queue;
            if (queue != null && queue.Contains(command)) {
                PlayerCommandGuard.Track(owner, command);
                Log.Engine.Debug($"Commands.Run queued {DescribeCommand(command)} for {owner.CharacterName} behind {DescribeBlocker(slots)} — engine runs it when the slot frees");
                return command;
            }
            Log.Engine.Warn($"Commands.Run discarded {DescribeCommand(command)} for {owner.CharacterName} (engine veto) — treating as not executed");
            return null;
        }

        static string DescribeCommand(UnitCommand cmd) {
            if (cmd is UnitUseAbility ua) return ua.Spell?.Name ?? "UnitUseAbility";
            return cmd.GetType().Name;
        }

        static string DescribeBlocker(UnitCommand[] slots) {
            for (int i = 0; i < slots.Length; i++) {
                var cmd = slots[i];
                if (cmd != null && cmd.IsStarted && !cmd.IsFinished)
                    return $"{DescribeCommand(cmd)} [{cmd.Type}]{(cmd.IsInterruptible ? "" : " (uninterruptible)")}";
            }
            return "a busy unit";
        }

        static bool ExecuteCastSpell(ActionDef action, UnitEntityData owner, ResolvedTarget target, out UnitCommand issuedCommand) {
            issuedCommand = null;
            ItemEntity inventorySource;
            string usedAbilityId;
            var ability = ActionValidator.ResolveCastSpellChain(owner, target, action, out inventorySource, out usedAbilityId);
            if (ability == null) {
                int chainLen = 1 + (action.FallbackAbilityIds?.Count ?? 0);
                Log.Engine.Warn($"ResolveCastSpellChain returned null for {owner.CharacterName}, primary={action.AbilityId}, chain={chainLen}");
                return false;
            }
            bool isFallback = usedAbilityId != action.AbilityId;
            if (isFallback)
                Log.Engine.Info($"CastSpell fallback hit for {owner.CharacterName}: primary={action.AbilityId} -> used={usedAbilityId}");

            var targetWrapper = BuildTargetWrapper(target, owner);

            // Rod activation (no-op when action.MetamagicRod == null or no rod matches).
            // Done after resolution, before any cast path — applies whether we end up in
            // the inventory branch or the spellbook branch.
            MaybeActivateRod(action, owner, ability);

            // Inventory source (scroll/potion): Kingmaker's own ItemEntity.TryUseFromInventory —
            // temp Ability fact with SourceItem (item caster level via GetParamsFromItem),
            // RuleCastSpell + InstantDeliver, AbilityData.Spend(), fact removed again
            // (engine-verification.md §7; user decision 2026-10-03). Mirror of ExecuteHeal.
            if (inventorySource != null) {
                try {
                    if (!inventorySource.TryUseFromInventory(owner, targetWrapper)) {
                        Log.Engine.Warn($"CastSpell: TryUseFromInventory refused {inventorySource.Blueprint.name} for {owner.CharacterName}");
                        return false;
                    }
                    string tgtDesc = target.IsPoint
                        ? $"point({target.Point.Value.x:F1},{target.Point.Value.z:F1})"
                        : (target.Unit?.CharacterName ?? "self");
                    Log.Engine.Info($"Cast (inventory): {inventorySource.Blueprint.name} -> {ability.Name} on {owner.CharacterName} -> {tgtDesc}");
                    return true;
                } catch (Exception ex) {
                    Log.Engine.Error(ex, $"CastSpell TryUseFromInventory failed for {inventorySource.Blueprint.name}");
                    return false;
                }
            }

            // Spellbook / Wand / class ability — animated cast command. Kingmaker's
            // CreateCastCommand never returns null (engine-verification.md §2), so Wrath's
            // Rulebook fallback is gone. Engine veto (unit CC'd/unconscious): the unit must
            // not act this tick; cooldown stays unstamped, retry next tick.
            var command = UnitUseAbility.CreateCastCommand(ability, targetWrapper);
            issuedCommand = RunVerified(owner, command);
            if (issuedCommand == null) return false;
            string castDesc = target.IsPoint
                ? $"point({target.Point.Value.x:F1},{target.Point.Value.z:F1})"
                : (target.Unit?.CharacterName ?? "self");
            Log.Engine.Debug($"Queued spell {ability.Name} on {owner.CharacterName} -> {castDesc}");
            return true;
        }

        /// <summary>
        /// If <paramref name="action"/>.MetamagicRod is set and a matching rod is equipped+quickslotted,
        /// activates the rod's ActivatableAbility on <paramref name="owner"/> so the next cast picks it up.
        /// Silent no-op when the field is null or no rod matches — caller proceeds with
        /// a normal cast.
        /// </summary>
        static void MaybeActivateRod(ActionDef action, UnitEntityData owner, AbilityData ability) {
            if (action.MetamagicRod == null) return;
            var mech = MetamagicRodResolver.TryResolve(owner, ability, action.MetamagicRod.Value);
            if (mech == null) return;
            var rodAbilityBp = mech.RodAbility;
            if (rodAbilityBp == null) return;
            foreach (var aa in owner.ActivatableAbilities) {
                if (aa.Blueprint != rodAbilityBp) continue;
                if (aa.IsOn) return; // already toggled — engine will spend the charge on the upcoming cast
                aa.TryStart();
                if (aa.IsOn) {
                    Log.Engine.Info($"Activated rod {rodAbilityBp.name} for {owner.CharacterName} ({mech.Metamagic} on {ability.Name})");
                }
                return;
            }
        }

        static bool ExecuteUseItem(string abilityGuid, UnitEntityData owner, ResolvedTarget target, out UnitCommand issuedCommand) {
            issuedCommand = null;
            var ability = ActionValidator.FindUseItemSource(owner, abilityGuid, out var inventorySource);
            if (ability == null) {
                Log.Engine.Warn($"Item ability {abilityGuid} not found on {owner.CharacterName}");
                return false;
            }

            var targetWrapper = BuildTargetWrapper(target, owner);

            // Inventory-backed potion/scroll: Kingmaker's native ItemEntity.TryUseFromInventory
            // (item caster level, charge spend, cleanup — engine-verification.md §7).
            if (inventorySource != null) {
                try {
                    if (!inventorySource.TryUseFromInventory(owner, targetWrapper)) {
                        Log.Engine.Warn($"UseItem: TryUseFromInventory refused {inventorySource.Blueprint.name} for {owner.CharacterName}");
                        return false;
                    }
                    Log.Engine.Info($"UseItem (inventory): {inventorySource.Blueprint.name} on {owner.CharacterName}");
                    return true;
                } catch (Exception ex) {
                    Log.Engine.Error(ex, $"UseItem TryUseFromInventory failed for {inventorySource.Blueprint.name}");
                    return false;
                }
            }

            // Equipped source (wand / scroll in quickslot) — animated cast path.
            var command = UnitUseAbility.CreateCastCommand(ability, targetWrapper);
            if (command == null) {
                Log.Engine.Warn($"CreateCastCommand failed for item ability");
                return false;
            }

            issuedCommand = RunVerified(owner, command);
            if (issuedCommand == null) return false;
            Log.Engine.Info($"Queued item use on {owner.CharacterName}");
            return true;
        }

        static bool ExecuteToggleActivatable(string abilityGuid, UnitEntityData owner, ToggleMode mode) {
            var activatable = ActionValidator.FindActivatable(owner, abilityGuid);
            if (activatable == null) {
                Log.Engine.Warn($"Activatable {abilityGuid} not found on {owner.CharacterName}");
                return false;
            }

            if (mode == ToggleMode.Off) {
                activatable.IsOn = false;
                Log.Engine.Info($"Toggled {activatable.Blueprint.name} OFF for {owner.CharacterName}");
            } else {
                // Kingmaker IL: the IsOn setter only flips m_IsOn, stamps m_TurnOnTime and calls
                // OnTurnOn — exactly what the game's own action-bar click does
                // (MechanicActionBarSlotActivableAbility.OnClick). Starting is the engine's job;
                // abilities with a start command (bardic performance) stay pending until it
                // acts, and the evaluator keeps their slots free (HoldPendingActivationSlots).
                activatable.IsOn = true;
                Log.Engine.Info($"Toggled {activatable.Blueprint.name} ON for {owner.CharacterName}");
            }
            return true;
        }

        static bool ExecuteHeal(ActionDef action, UnitEntityData owner, UnitEntityData target, out UnitCommand issuedCommand) {
            issuedCommand = null;
            ItemEntity inventorySource;
            // target ?? owner: mirror the TargetWrapper fallback below for self-heal-on-no-target.
            // Auto-mode in FindBestHealEx checks the resolved unit for negative-energy affinity.
            var ability = ActionValidator.FindBestHealEx(
                owner,
                target ?? owner,
                action.HealMode,
                action.HealSources,
                action.HealEnergy,
                out inventorySource);
            if (ability == null) {
                Log.Engine.Warn($"FindBestHeal returned null for {owner.CharacterName}");
                return false;
            }

            var targetWrapper = target != null
                ? new TargetWrapper(target)
                : new TargetWrapper(owner);

            // Inventory potions/scrolls: Kingmaker's native ItemEntity.TryUseFromInventory
            // (item caster level, charge spend, cleanup — engine-verification.md §7).
            if (inventorySource != null) {
                try {
                    if (!inventorySource.TryUseFromInventory(owner, targetWrapper)) {
                        Log.Engine.Warn($"Heal: TryUseFromInventory refused {inventorySource.Blueprint.name} for {owner.CharacterName}");
                        return false;
                    }
                    Log.Engine.Info($"Heal (inventory): {inventorySource.Blueprint.name} on {owner.CharacterName} -> {target?.CharacterName ?? "self"}");
                    return true;
                } catch (Exception ex) {
                    Log.Engine.Error(ex, $"Heal TryUseFromInventory failed for {inventorySource.Blueprint.name}");
                    return false;
                }
            }

            // Spellbook spell, class ability, or quickslot wand — animated cast path.
            var command = UnitUseAbility.CreateCastCommand(ability, targetWrapper);
            issuedCommand = RunVerified(owner, command);
            if (issuedCommand == null) return false;
            Log.Engine.Info($"Heal (animated): {ability.Name} on {owner.CharacterName} -> {target?.CharacterName ?? "self"}");
            return true;
        }

        static bool ExecuteThrowSplash(ActionDef action, UnitEntityData owner, UnitEntityData target,
                                       out UnitCommand issuedCommand) {
            issuedCommand = null;
            if (target == null) {
                Log.Engine.Warn($"ThrowSplash: no target for {owner.CharacterName}");
                return false;
            }

            var pick = SplashItemResolver.FindBest(owner, action.SplashMode);
            if (!pick.HasValue) {
                Log.Engine.Warn($"ThrowSplash: no splash items available for {owner.CharacterName}");
                return false;
            }

            // Quick-slot flask: the engine's own cast command, as when the player clicks the
            // belt slot (animated, spends the stack through the ability's SourceItem).
            if (pick.Value.QuickSlot != null) {
                var command = UnitUseAbility.CreateCastCommand(pick.Value.QuickSlot, new TargetWrapper(target));
                issuedCommand = RunVerified(owner, command);
                if (issuedCommand == null) return false;
                Log.Engine.Info($"ThrowSplash: {owner.CharacterName} throws {pick.Value.Name} (quick slot) at {target.CharacterName}");
                return true;
            }

            var item = pick.Value.Item;
            var usable = item.Blueprint as BlueprintItemEquipmentUsable;
            if (usable == null) {
                Log.Engine.Warn($"ThrowSplash: {item.Blueprint.name} is not a usable item");
                return false;
            }

            // Kingmaker's native inventory use: temp Ability fact with SourceItem (item caster
            // level), instant RuleCastSpell, Spend(), cleanup (engine-verification.md §7).
            var tw = new TargetWrapper(target);

            try {
                if (!item.TryUseFromInventory(owner, tw)) {
                    Log.Engine.Warn($"ThrowSplash: TryUseFromInventory refused {item.Blueprint.name} for {owner.CharacterName}");
                    return false;
                }
                Log.Engine.Info($"ThrowSplash: {owner.CharacterName} threw {item.Blueprint.name} at {target.CharacterName}");
                return true;
            } catch (Exception ex) {
                Log.Engine.Error(ex, $"ThrowSplash TryUseFromInventory failed for {item.Blueprint.name}");
                return false;
            }
        }

        // UnitSwitchHandEquipmentSet IL (ctor + OnAction): Kingmaker CommandType=Move
        // (engine-verification.md §1; Wrath: Free), OnAction calls
        // unit.Body.set_CurrentHandEquipmentSetIndex(idx). It IS a real UnitCommand (queued via
        // Commands.Run), so ActiveRuleTracker can gate on it via issuedCommand. Engine respects
        // Quick Draw and reach-feat timing downstream — we don't model action economy here.
        static bool ExecuteSwitchWeaponSet(int targetIndex, UnitEntityData owner, out UnitCommand issuedCommand) {
            issuedCommand = null;
            // Defensive — validator already checked, but ExecuteSwitchWeaponSet can be reached
            // through code paths that bypass validation (preset edits, manual rule replay) so
            // we re-check rather than trust the upstream.
            if (!ActionValidator.CanSwitchWeaponSet(owner, targetIndex)) {
                Log.Engine.Warn($"ExecuteSwitchWeaponSet: validator rejected index {targetIndex} for {owner.CharacterName}");
                return false;
            }
            var command = new UnitSwitchHandEquipmentSet(targetIndex);
            issuedCommand = RunVerified(owner, command);
            if (issuedCommand == null) return false;
            Log.Engine.Info($"SwitchWeaponSet: {owner.CharacterName} -> set {targetIndex}");
            return true;
        }

        static bool ExecuteMoveToTarget(RangeBracket within, UnitEntityData owner, ResolvedTarget target, out UnitCommand issuedCommand) {
            issuedCommand = null;
            if (!ActionValidator.TryGetMoveDestination(owner, target, out var destination, out float distance)) return false;
            // approachRadius = the bracket's outer edge: the engine stops the walk once the
            // unit is inside it (Kingmaker measures XZ already — no distanceXZ flag,
            // engine-verification.md §10). Re-evaluated every tick, so a moving target is
            // followed until the bracket holds.
            var command = new UnitMoveTo(destination, RangeBrackets.MaxMeters(within));
            issuedCommand = RunVerified(owner, command);
            if (issuedCommand == null) return false;
            string where = target.Unit != null ? target.Unit.CharacterName : $"point({destination.x:F1},{destination.z:F1})";
            Log.Engine.Info($"MoveToTarget: {owner.CharacterName} -> {where}, {distance:F1} m, stop within {within}");
            return true;
        }

        static bool ExecuteAttack(UnitEntityData owner, UnitEntityData target, out UnitCommand issuedCommand) {
            issuedCommand = null;
            if (target == null) return false;

            // Same factory as a player click (handles Magus Spell Combat; engine-verification.md §10).
            var command = UnitAttack.CreateAttackCommand(owner, target);
            issuedCommand = RunVerified(owner, command);
            if (issuedCommand == null) return false;
            Log.Engine.Info($"Queued attack on {owner.CharacterName} -> {target.CharacterName}");
            return true;
        }
    }
}
