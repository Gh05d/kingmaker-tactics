using System;
using Kingmaker.EntitySystem.Entities;
using KingmakerTactics.Persistence;
using TMPro;
using UnityEngine;

namespace KingmakerTactics.UI {
    // One badge per portrait cell. Cells get re-bound to another unit on paging and party
    // changes (GroupController.SetGroup → GroupCharacter.Initialize), so the unit is re-read
    // through the getter on EVERY refresh and click — never cached.
    public class PortraitToggleBadge : MonoBehaviour {
        Func<UnitEntityData> currentUnit;
        TextMeshProUGUI label;

        public void Init(Func<UnitEntityData> unitGetter, TextMeshProUGUI stateLabel) {
            currentUnit = unitGetter;
            label = stateLabel;
        }

        UnitEntityData CurrentUnit() {
            try { return currentUnit?.Invoke(); }
            catch (Exception) { return null; } // cell torn down between discovery and refresh
        }

        public void Refresh(bool show) {
            var config = ConfigManager.Current;
            var state = PortraitBadges.StateFor(show, CurrentUnit()?.UniqueId, config.IsEnabled);
            if (state == PortraitBadgeState.Hidden) {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            bool on = state == PortraitBadgeState.On;
            label.color = on ? Theme.BadgeOn : Color.gray;
            label.fontStyle = on ? FontStyles.Normal : FontStyles.Strikethrough;
        }

        public void OnClick() {
            var unit = CurrentUnit();
            if (unit == null) return;
            var config = ConfigManager.Current;
            config.TacticsEnabled[unit.UniqueId] = !config.IsEnabled(unit.UniqueId);
            ConfigManager.Save();
            TacticsPanel.NotifyExternalConfigChange();
            Refresh(config.ShowPortraitToggles);
        }
    }
}
