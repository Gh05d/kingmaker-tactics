using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Kingmaker.UI._ConsoleUI.HudGroup;
using KingmakerTactics.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerTactics.UI {
    // Attaches a PortraitToggleBadge to every party portrait cell of Kingmaker's legacy UI.
    // Discovery is component-type based (cells are prefab clones, no stable name path) and
    // throttled; per-frame work is only the cheap state refresh. Polling instead of a
    // Harmony postfix on GroupCharacter.Initialize: survives canvas rebuilds on area/save
    // load without depending on which init path the game takes.
    //
    // Two view trees (IL, kingmaker/il-dump):
    // - PC/mouse HUD: GroupController owns six Kingmaker.UI.Group.GroupCharacter cells
    //   (also GlobalMapGroupCharacter), unit = CharacterBase.Unit, re-bound in SetGroup.
    // - Controller UI: HudGroupView builds one HudGroupCharacterView per character,
    //   unit = ViewModel.Unit (ViewModel is protected — publicized Assembly-CSharp).
    // The in-game HUD never lists pets (GroupController uses UIUtility.GetGroup(_, WithPet)
    // with WithPet only for inventory/character/spellbook screens), so pets are toggled in
    // the panel only.
    public static class PortraitToggleOverlay {
        const string BadgeName = "KT_PortraitToggle";
        const float DiscoveryInterval = 1f;
        static float discoveryTimer;
        static readonly List<PortraitToggleBadge> badges = new List<PortraitToggleBadge>();

        public static void Sync(float delta) {
            if (Game.Instance?.UI?.Canvas == null) return;

            discoveryTimer -= delta;
            if (discoveryTimer <= 0f) {
                discoveryTimer = DiscoveryInterval;
                Discover();
            }

            bool show = ConfigManager.Current.ShowPortraitToggles;
            for (int i = badges.Count - 1; i >= 0; i--) {
                var badge = badges[i];
                if (badge == null) { badges.RemoveAt(i); continue; } // cell destroyed with its canvas
                badge.Refresh(show);
            }
        }

        // Badges live under GAME-owned portrait cells, so mod unload must destroy them
        // explicitly or they survive as clickable orphans that keep writing config.
        public static void Cleanup() {
            for (int i = badges.Count - 1; i >= 0; i--) {
                if (badges[i] != null)
                    UnityEngine.Object.Destroy(badges[i].gameObject);
            }
            badges.Clear();
            discoveryTimer = 0f;
        }

        static void Discover() {
            // FontScale is refreshed lazily by TacticsPanel.Toggle(); badges are usually
            // created before the panel was ever opened.
            UIHelpers.RefreshFontScale();

            foreach (var cell in UnityEngine.Object.FindObjectsOfType<GroupCharacter>()) {
                var c = cell;
                EnsureBadge(c.transform, () => c == null ? null : c.Unit);
            }
            foreach (var view in UnityEngine.Object.FindObjectsOfType<HudGroupCharacterView>()) {
                var v = view;
                EnsureBadge(v.transform, () => v == null ? null : v.ViewModel?.Unit);
            }
        }

        static void EnsureBadge(Transform cell, Func<UnitEntityData> unit) {
            if (cell.Find(BadgeName) != null) return;

            var (go, rect) = UIHelpers.Create(BadgeName, cell);
            rect.SetAnchor(0, 0, 1, 1); // point-anchor at the cell's top-left corner
            // 26 px: the 22 px Wrath size read too small on Kingmaker's ~110 px cells (deck
            // screenshot 2026-10-03) and is hard to hit with the Steam Deck trackpad. Top-left is
            // free: the HP bar starts below it, status icons sit top-right.
            float size = 26f * UIHelpers.FontScale;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(size * 0.5f + 2f, -(size * 0.5f + 2f));
            go.transform.SetAsLastSibling(); // draw above the portrait frame

            UIHelpers.AddBackground(go, Theme.DimBackdrop);
            var label = Widgets.BandLabel(go, "T", 18f, TextAlignmentOptions.Midline);

            var badge = go.AddComponent<PortraitToggleBadge>();
            badge.Init(unit, label);
            // A child Button is the nearest click handler, so the portrait's own
            // OnPointerClick (select unit / cast onto portrait) does not fire for this click.
            go.AddComponent<Button>().onClick.AddListener(badge.OnClick);

            badges.Add(badge);
        }
    }
}
