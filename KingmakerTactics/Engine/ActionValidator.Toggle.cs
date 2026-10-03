using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using KingmakerTactics.Models;

namespace KingmakerTactics.Engine {
    public enum ToggleVerdict { Ok, NotOwned, AlreadyInState, Unavailable }

    public static partial class ActionValidator {
        static bool CanToggleActivatable(string abilityGuid, UnitEntityData owner, ToggleMode mode) {
            var activatable = FindActivatable(owner, abilityGuid);
            var verdict = JudgeToggle(activatable != null, activatable?.IsOn ?? false,
                activatable?.IsAvailable ?? false, mode);
            switch (verdict) {
                case ToggleVerdict.Ok: return true;
                case ToggleVerdict.AlreadyInState:
                    Reject(mode == ToggleMode.On ? "already on" : "already off", satisfied: true);
                    return false;
                case ToggleVerdict.NotOwned:
                    Reject("unit does not have this ability");
                    return false;
                default:
                    Reject("ability currently unavailable (resource, weapon or restriction)");
                    return false;
            }
        }

        /// <summary>Pure decision behind CanToggleActivatable. AlreadyInState is the rule's
        /// success state (song running, rage on) and must not be reported as a failure.</summary>
        public static ToggleVerdict JudgeToggle(bool owned, bool isOn, bool isAvailable, ToggleMode mode) {
            if (!owned) return ToggleVerdict.NotOwned;
            if (mode == ToggleMode.Off) return isOn ? ToggleVerdict.Ok : ToggleVerdict.AlreadyInState;
            if (isOn) return ToggleVerdict.AlreadyInState;
            return isAvailable ? ToggleVerdict.Ok : ToggleVerdict.Unavailable;
        }

        public static ActivatableAbility FindActivatable(UnitEntityData owner, string abilityGuid) {
            if (string.IsNullOrEmpty(abilityGuid)) return null;
            return owner.ActivatableAbilities.Enumerable
                .FirstOrDefault(a => a.Blueprint.AssetGuid.ToString() == abilityGuid);
        }
    }
}
