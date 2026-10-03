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
        const float Inset = 2f;
        static readonly Vector3[] corners = new Vector3[4];
        Func<UnitEntityData> currentUnit;
        TextMeshProUGUI label;
        RectTransform portraitImage;

        public void Init(Func<UnitEntityData> unitGetter, TextMeshProUGUI stateLabel, RectTransform portrait) {
            currentUnit = unitGetter;
            label = stateLabel;
            portraitImage = portrait;
        }

        // The badge stays a child of the cell (drawn last, above frame and overlays) but is
        // placed from the portrait IMAGE's top-left corner: the cell rect includes the gold
        // frame and the HP bar, where a cell-corner badge sat (deck screenshot 2026-10-03).
        // Re-done every refresh — layout is not final when the badge is created.
        void AlignToPortrait() {
            if (portraitImage == null) return;
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            portraitImage.GetWorldCorners(corners); // [1] = top-left
            Vector2 topLeft = parent.InverseTransformPoint(corners[1]);
            var center = PortraitBadges.CenterFromTopLeft(topLeft, rect.sizeDelta.x, Inset);
            rect.localPosition = new Vector3(center.x, center.y, 0f);
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
            AlignToPortrait();
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
