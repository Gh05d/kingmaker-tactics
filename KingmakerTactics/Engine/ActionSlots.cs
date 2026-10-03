using Kingmaker.UnitLogic.Commands.Base;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    /// <summary>
    /// Maps a rule's ActionType — plus, for ability-backed types, the resolved
    /// AbilityData.RuntimeActionType — onto the UnitCommand slot the rule occupies.
    /// Pure: no engine state, no side effects, fully unit-testable.
    ///
    /// UnitCommand.CommandType is Free = 0, Standard = 1, Swift = 2, Move = 3, and
    /// UnitCommands.m_Commands is indexed by it, so (int)slot doubles as a budget index.
    /// </summary>
    internal static class ActionSlots {
        internal static UnitCommand.CommandType? Classify(
            ActionType type, UnitCommand.CommandType? abilitySlot) {
            switch (type) {
                case ActionType.CastSpell:
                case ActionType.CastAbility:
                case ActionType.UseItem:
                case ActionType.Heal:
                    // RuntimeActionType already folds in Quicken (Swift -> Standard once the
                    // swift action is spent); Kingmaker verification: engine-verification.md §1.
                    // An unresolvable slot degrades to Standard so a classification miss
                    // behaves like the old one-action-per-tick evaluator instead of escaping
                    // the priority gate.
                    return abilitySlot ?? UnitCommand.CommandType.Standard;

                case ActionType.AttackTarget:
                    return UnitCommand.CommandType.Standard;

                // ThrowSplash bypasses Commands.Run entirely (Rulebook.Trigger plus manual
                // stack consumption), so it occupies no engine slot. It still claims Standard
                // in the tick budget: a thrown flask IS a standard action, and leaving it
                // unclaimed would let it fire on top of a cast AND an attack every tick.
                case ActionType.ThrowSplash:
                    return UnitCommand.CommandType.Standard;

                // Kingmaker: UnitSwitchHandEquipmentSet is CommandType.Move (engine-verification.md §1;
                // Wrath: Free). Run(Move) removes the paired Standard, hence the cross-slot check.
                case ActionType.SwitchWeaponSet:
                    return UnitCommand.CommandType.Move;

                // UnitMoveTo lives in the Move slot. Not animated, but Run(Move) removes the
                // paired Standard command, so it goes through the cross-slot check.
                case ActionType.MoveToTarget:
                    return UnitCommand.CommandType.Move;

                // Sets ActivatableAbility.IsOn — issues no command, so it claims nothing and
                // is exempt from both the gate and the budget. A toggle rule stops matching
                // once its activatable reaches the requested state, so this does not spam.
                case ActionType.ToggleActivatable:
                    return null;

                // Claiming Standard is cosmetic — DoNothing hard-stops the whole tick — but
                // it keeps the classification total and honest.
                case ActionType.DoNothing:
                    return UnitCommand.CommandType.Standard;

                default:
                    return UnitCommand.CommandType.Standard;
            }
        }

        /// <summary>
        /// Slots an activatable with a start command (Kingmaker bardic performance) must keep free.
        /// KM IL ActivatableAbility.HandleUnitRunCommand: while IsOn, any command the unit runs
        /// whose Type equals the activatable's ActivatableAbilityUnitCommand.Type switches it off
        /// — running or not. While the activation is still pending, a Standard/Move command also
        /// removes the paired, not-yet-acted activation command, which HandleUnitCommandDidEnd
        /// turns into IsOn = false as well.
        /// </summary>
        internal static UnitCommand.CommandType[] HeldByActivation(UnitCommand.CommandType activationType, bool running) {
            if (!running && activationType == UnitCommand.CommandType.Standard)
                return new[] { UnitCommand.CommandType.Standard, UnitCommand.CommandType.Move };
            if (!running && activationType == UnitCommand.CommandType.Move)
                return new[] { UnitCommand.CommandType.Move, UnitCommand.CommandType.Standard };
            return new[] { activationType };
        }

        /// <summary>
        /// An activatable with a start command holds its slots while it is on and either running
        /// or still able to start: available and switched on less than one round (6 s) ago. The
        /// time cap keeps an activation the engine never starts from blocking the unit's other
        /// rules for the rest of the fight.
        /// </summary>
        internal static bool IsActivationHolding(bool isOn, bool isRunning, bool hasStartCommand,
            bool isAvailable, float secondsSinceTurnOn) {
            if (!isOn || !hasStartCommand) return false;
            return isRunning || (isAvailable && secondsSinceTurnOn < 6f);
        }

        /// <summary>
        /// True for the slot the ActiveRuleTracker priority gate governs. Only Standard-slot
        /// rules participate in DAO-style preemption; move/swift/free rules bypass the gate.
        /// </summary>
        internal static bool IsGated(UnitCommand.CommandType? slot) {
            return slot == UnitCommand.CommandType.Standard;
        }

        /// <summary>
        /// True for rule types whose execution issues an animated UnitCommand
        /// (UnitUseAbility or UnitAttack). Only these can collide on the unit's
        /// AnimationManager — see CheckConflict.
        /// </summary>
        internal static bool IssuesAnimatedCommand(ActionType type) {
            switch (type) {
                case ActionType.CastSpell:
                case ActionType.CastAbility:
                case ActionType.UseItem:
                case ActionType.Heal:
                case ActionType.AttackTarget:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Rule types whose command must pass HasCrossSlotConflict: every animated command,
        /// plus the Move-slot commands MoveToTarget and (Kingmaker) SwitchWeaponSet — neither
        /// plays a cast/attack animation, but issuing either removes an own pending or running
        /// Standard command through the paired-slot rule.
        /// </summary>
        internal static bool NeedsCrossSlotCheck(ActionType type) {
            return IssuesAnimatedCommand(type) || type == ActionType.MoveToTarget || type == ActionType.SwitchWeaponSet;
        }

        /// <summary>
        /// Engine action budget (v1.30). The engine books acted commands into
        /// UnitCombatState.Cooldown — Kingmaker RTWP: a standard action sets
        /// StandardAction = 6 − TimeSinceStart, free and move actions set
        /// MoveAction = 3 − TimeSinceStart (UnitActionController.UpdateCooldowns,
        /// engine-verification.md §1; Wrath books differently) — and HasCooldownForCommand(type)
        /// is its verdict on "is this action still available this round". A rule whose slot is
        /// spent must not issue: the command would only buffer in its slot, and a buffered
        /// Standard blocks every Move rule through the paired-slot rule for the whole
        /// cooldown (the 1.29.x "Cackle never fires without cooldowns" report).
        ///
        /// Buffering is still allowed when the action frees before the next evaluation
        /// tick: the command then starts exactly when the engine allows it, and no other
        /// slot lost a tick to it. Re-evaluating at the next tick would only add latency.
        /// </summary>
        internal static bool ActionSpent(bool slotOnCooldown, float remainingSeconds, float tickIntervalSeconds) {
            return slotOnCooldown && remainingSeconds > tickIntervalSeconds;
        }

        /// <summary>
        /// Rule types whose command the engine starts regardless of action cooldowns.
        /// UnitCommandController.ShouldStartCommand returns true for UnitMoveTo (and area
        /// transitions) BEFORE its HasCooldownForCommand check — walking is free in RTWP,
        /// exactly as a player's ground click right after a cast. Gating a walk on the move
        /// action made it wait up to 3 s for nothing (Nexus, 1.31.0 follow-up).
        /// </summary>
        internal static bool UsesActionBudget(ActionType type) {
            return type != ActionType.MoveToTarget;
        }

        /// <summary>
        /// A running Swift does NOT hold a pending Standard back (the engine's Standard start
        /// check only looks at a running Move). So a Swift may overlap a pending Standard only
        /// when the Standard is guaranteed to still be cooling down after the swift animation
        /// has finished. 2.5 s comfortably covers every quick-cast animation.
        /// </summary>
        internal const float SwiftOverlapMinStandardCooldown = 2.5f;

        /// <summary>
        /// Decides whether issuing a command in <paramref name="issuing"/> would destroy or be
        /// destroyed by an unfinished animated command (UnitUseAbility / UnitAttack) currently
        /// sitting in <paramref name="occupied"/>. The caller guarantees the two slots differ
        /// and the occupant is animated and not finished; <paramref name="occupantApproaching"/>
        /// is <c>!occupant.IsStarted &amp;&amp; !occupant.IsUnitCloseEnough()</c>,
        /// <paramref name="occupantOwn"/> is <c>PlayerCommandGuard.IsOurs(occupant)</c>,
        /// <paramref name="occupantIsCast"/> is <c>occupant is UnitUseAbility</c>.
        /// Same-slot conflicts are the budget's and the priority gate's business.
        ///
        /// Two engine facts drive this (both IL-verified, both learnt the hard way in 1.29.0):
        ///
        /// 1. <b>Move and Standard are a PAIR, not independent slots.</b>
        ///    <c>UnitCommands.Run(cmd)</c> calls <c>InterruptAndRemoveCommand(cmd.Type)</c>,
        ///    whose one-argument overload also removes the paired slot: a Move command
        ///    interrupts whatever sits in Standard (pending or running) and vice versa.
        ///    So a Move ability issued over our own pending cast simply deletes the cast —
        ///    no cooldown margin makes that safe. Over an ENGINE-issued Standard command
        ///    (auto-attack, the unit's default action) it is exactly what clicking the
        ///    ability does: the engine's command dies, ours runs, the party AI re-issues
        ///    its own afterwards. Player casts never reach this point — PlayerCommandGuard
        ///    skips the whole unit while one is in the slot or the queue.
        ///
        /// 2. <b>Two animated commands must never run at once.</b> Slots tick in array order
        ///    and each unstarted command starts as soon as its own cooldown allows; two
        ///    running commands share one AnimationManager, the newer animation releases the
        ///    older one and <c>UnitCommand.Tick</c> interrupts it as "done but not acted".
        ///    The engine's only cross-slot hold is Standard-waits-while-Move-IsRunning.
        /// </summary>
        internal static SlotConflict CheckConflict(
            UnitCommand.CommandType issuing,
            UnitCommand.CommandType occupied,
            bool occupantStarted,
            bool occupantApproaching,
            bool occupantOwn,
            bool occupantIsCast,
            bool issuingSlotOnCooldown,
            float standardCooldownRemaining) {
            // Fact 1: Run(Move) removes the Standard command outright.
            if (issuing == UnitCommand.CommandType.Move && occupied == UnitCommand.CommandType.Standard) {
                if (occupantOwn) return SlotConflict.PairedOwn;
                // A foreign cast that has already started (the party AI's default action,
                // e.g. Ray of Frost) is seconds from acting. Issuing a Move now does not
                // even remove it cleanly: Run queues ours behind the uninterruptible cast
                // and flags it InterruptAsSoonAsPossible, which TickCommand honours during
                // the wind-up — the cast dies unacted (deck 2026-09-16: 10 of 10 Cackles
                // killed a Ray). Wait; the budget gate then hands the next tick to us.
                // A PENDING foreign cast or an auto-attack in any state is fair game: the
                // AI re-issues it after our Move, exactly as after a player click.
                if (occupantIsCast && occupantStarted) return SlotConflict.Running;
                return SlotConflict.None;
            }

            // Fact 2: a started, unfinished animated command owns the AnimationManager.
            // (Also covers Standard-over-Move: Run(Standard) removes the Move command, and
            // an own Move ability in flight must be allowed to finish.)
            if (occupantStarted) return SlotConflict.Running;

            // A pending command that is still walking towards its target is held by
            // distance, not by a cooldown. Run(cmd) interrupts every unstarted command with
            // !IsUnitCloseEnough() whenever cmd is Standard or Move (Swift/Free only when cmd
            // itself is out of reach), so issuing now cancels that approach.
            if (occupantApproaching) return SlotConflict.Approaching;

            // Pending Standard occupant, Swift issuing: Swift is not paired with Standard,
            // but nothing holds Standard back from a running Swift either, so the Standard
            // cooldown must outlast the swift animation and our Swift must start this frame.
            if (issuing == UnitCommand.CommandType.Swift
                && occupied == UnitCommand.CommandType.Standard
                && !issuingSlotOnCooldown
                && standardCooldownRemaining > SwiftOverlapMinStandardCooldown) {
                return SlotConflict.None;
            }

            // Every other pending combination either starts in the same frame as ours
            // (Standard ticks before Swift/Move, so a cooldown-free pending Standard starts
            // first and our command lands on top of it) or starts later, underneath our
            // running command (a pending Move/Swift ignores a running Standard). Wait a tick.
            return SlotConflict.Pending;
        }
    }

    /// <summary>Outcome of ActionSlots.CheckConflict.</summary>
    internal enum SlotConflict {
        None,
        /// <summary>An animated command in another slot has started and not finished.</summary>
        Running,
        /// <summary>An animated command in another slot is waiting to start and would
        /// collide with ours once either of them starts.</summary>
        Pending,
        /// <summary>An animated command in another slot has not started because its
        /// executor is still walking into range; issuing ours would make
        /// UnitCommands.Run cancel that approach.</summary>
        Approaching,
        /// <summary>Our own command sits in the slot paired with the issuing one
        /// (Move ↔ Standard); UnitCommands.Run would remove it outright.</summary>
        PairedOwn,
    }
}
