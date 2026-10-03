using Kingmaker.Utility;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerTactics.Logging;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    public static partial class ActionValidator {
        /// <summary>Why the last CanExecute returned false, for the evaluator's WARN; null when
        /// no validator branch recorded a reason. LastRejectSatisfied marks a rejection that
        /// is really the desired end state (toggle already on/off) — not worth a WARN.</summary>
        public static string LastRejectReason { get; private set; }
        public static bool LastRejectSatisfied { get; private set; }

        static void Reject(string reason, bool satisfied = false) {
            LastRejectReason = reason;
            LastRejectSatisfied = satisfied;
        }

        // `abilitySlot` carries the resolved ability's RuntimeActionType so the evaluator can
        // tell which UnitCommand slot the rule will occupy. It stays null for action types
        // that are not ability-backed and on every `false` return; ActionSlots.Classify
        // supplies the fallback. Validation logic below is unchanged — same conditions, same
        // ordering, same log lines.
        public static bool CanExecute(ActionDef action, UnitEntityData owner, ResolvedTarget target,
                                      out UnitCommand.CommandType? abilitySlot) {
            abilitySlot = null;
            LastRejectReason = null;
            LastRejectSatisfied = false;

            if (!target.IsValid && RequiresValidTarget(action.Type)) {
                Reject("no valid target");
                return false;
            }

            if (target.IsPoint) {
                switch (action.Type) {
                    case ActionType.CastSpell:
                    case ActionType.CastAbility: {
                        ItemEntity _unused;
                        string _unusedId;
                        var ability = ResolveCastSpellChain(owner, target, action, out _unused, out _unusedId);
                        if (ability == null) return false;
                        if (!ability.Blueprint.CanTargetPoint) {
                            Log.Engine.Trace($"CanCastAbilityAtPoint: {owner.CharacterName} ability '{ability.Name}' is not point-castable");
                            return false;
                        }
                        abilitySlot = ability.RuntimeActionType;
                        return true;
                    }
                    case ActionType.UseItem: {
                        if (!CanUseItemAtPoint(action.AbilityId, owner, out var itemAbility)) return false;
                        abilitySlot = itemAbility.RuntimeActionType;
                        return true;
                    }
                    case ActionType.MoveToTarget:
                        return CanMoveToTarget(owner, target, action.MoveWithin);
                    default:
                        return false;
                }
            }

            var unit = target.Unit;
            switch (action.Type) {
                case ActionType.CastSpell:
                case ActionType.CastAbility: {
                    ItemEntity _unused;
                    string _unusedId;
                    var ability = ResolveCastSpellChain(owner, target, action, out _unused, out _unusedId);
                    if (ability == null) {
                        Reject("no castable source (not known, no slot left, no usable scroll/wand/potion)");
                        return false;
                    }
                    // The engine's own legality check for THIS target (target-type flags and
                    // IAbilityTargetRestriction components such as "not hexed within 24 h").
                    // Without it the cast is issued, UnitUseAbility.OnTick self-interrupts it
                    // unacted a frame after it starts, and the rule burns its tick for nothing
                    // (Camellia's Misfortune on an already-hexed treant, deck 2026-09-16).
                    // Point-capable spells (Fireball, Grease) are cast AT the unit's position and
                    // may carry no unit-target flags at all — leave them to the old path.
                    if (unit != null && !ability.Blueprint.CanTargetPoint && !ability.CanTarget(new TargetWrapper(unit))) {
                        Log.Engine.Trace($"CanExecute: {owner.CharacterName} '{ability.Name}' cannot target {unit.CharacterName} (engine CanTarget)");
                        return false;
                    }
                    abilitySlot = ability.RuntimeActionType;
                    return true;
                }
                case ActionType.UseItem: {
                    if (!CanUseItem(action.AbilityId, owner, unit, out var itemAbility)) return false;
                    abilitySlot = itemAbility.RuntimeActionType;
                    return true;
                }
                case ActionType.ToggleActivatable:
                    return CanToggleActivatable(action.AbilityId, owner, action.ToggleMode);
                case ActionType.AttackTarget:
                    return unit != null && unit.HPLeft > 0;
                case ActionType.Heal: {
                    // Self-heal when no explicit target is resolved — mirrors ExecuteHeal's
                    // `target ?? owner` fallback. Auto-mode reads the unit for affinity check.
                    var heal = FindBestHeal(owner, unit ?? owner, action.HealMode, action.HealSources, action.HealEnergy);
                    if (heal == null) return false;
                    abilitySlot = heal.RuntimeActionType;
                    return true;
                }
                case ActionType.ThrowSplash: {
                    if (unit == null) return false;
                    var pick = SplashItemResolver.FindBest(owner, action.SplashMode);
                    if (!pick.HasValue) {
                        Reject("no throwable splash item (in combat only quick-slot items can be used)");
                        return false;
                    }
                    abilitySlot = pick.Value.QuickSlot?.RuntimeActionType;
                    return true;
                }
                case ActionType.SwitchWeaponSet:
                    return CanSwitchWeaponSet(owner, action.WeaponSetIndex);
                case ActionType.MoveToTarget:
                    return CanMoveToTarget(owner, target, action.MoveWithin);
                case ActionType.DoNothing:
                    return true;
                default:
                    return false;
            }
        }

        static bool RequiresValidTarget(ActionType type) {
            return type != ActionType.ToggleActivatable
                && type != ActionType.Heal
                && type != ActionType.DoNothing
                && type != ActionType.SwitchWeaponSet;
        }
    }
}
