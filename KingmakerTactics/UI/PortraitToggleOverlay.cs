using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Kingmaker.UI._ConsoleUI.HudGroup;
using KingmakerTactics.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerTactics.UI {
    // Attaches a PortraitToggleBadge to every party portrait cell of Kingmaker's UI. Cells
    // announce themselves through Harmony postfixes on their bind methods and are queued;
    // Sync attaches badges to queued cells on the main thread, per-frame work is only the
    // cheap state refresh. NEVER discover cells with Object.FindObjectsOfType: Kingmaker
    // keeps the whole blueprint library resident, a single scan costs ~90 ms on the deck and
    // the former 1 s poll (two scans) was a 185 ms hitch every second (Nexus report
    // 2026-10-06, FrameProbe "discover" section).
    //
    // Two view trees (IL, kingmaker/il-dump):
    // - PC/mouse HUD: GroupController.SetGroup → GroupCharacter.Initialize(unit, index) for
    //   each of its six cells, also on the global map (GlobalMapGroupCharacter does not
    //   override it). Unit = CharacterBase.Unit, re-bound on every Initialize.
    // - Controller UI: HudGroupView builds one HudGroupCharacterView per character and calls
    //   Bind(GroupCharacterVM) (GlobalMapHudGroupCharacterView.Bind calls base). Unit =
    //   ViewModel.Unit (ViewModel is protected — publicized Assembly-CSharp).
    // Re-binds re-queue the same cell; EnsureBadge skips cells that already carry a badge.
    // The in-game HUD never lists pets (GroupController uses UIUtility.GetGroup(_, WithPet)
    // with WithPet only for inventory/character/spellbook screens), so pets are toggled in
    // the panel only.
    public static class PortraitToggleOverlay {
        const string BadgeName = "KT_PortraitToggle";
        static readonly List<PortraitToggleBadge> badges = new List<PortraitToggleBadge>();
        static readonly List<GroupCharacter> pendingCells = new List<GroupCharacter>();
        static readonly List<HudGroupCharacterView> pendingViews = new List<HudGroupCharacterView>();

        public static void Sync(float delta) {
            if (Game.Instance?.UI?.Canvas == null) return;

            if (pendingCells.Count > 0 || pendingViews.Count > 0) {
                var t = Logging.FrameProbe.Start();
                AttachPending();
                Logging.FrameProbe.Add(Logging.FrameProbe.Section.Discover, t);
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
            pendingCells.Clear();
            pendingViews.Clear();
        }

        static void AttachPending() {
            // FontScale is refreshed lazily by TacticsPanel.Toggle(); badges are usually
            // created before the panel was ever opened.
            UIHelpers.RefreshFontScale();

            foreach (var cell in pendingCells) {
                if (cell == null) continue; // destroyed before this frame
                var c = cell;
                EnsureBadge(c.transform, c.Portrait?.Portrait?.rectTransform, () => c == null ? null : c.Unit);
            }
            pendingCells.Clear();
            foreach (var view in pendingViews) {
                if (view == null) continue;
                var v = view;
                EnsureBadge(v.transform, v.m_PortraitPartView?.m_Portrait?.rectTransform, () => v == null ? null : v.ViewModel?.Unit);
            }
            pendingViews.Clear();
        }

        [HarmonyPatch(typeof(GroupCharacter), nameof(GroupCharacter.Initialize), new[] { typeof(UnitEntityData), typeof(int) })]
        static class GroupCharacterInitializePatch {
            static void Postfix(GroupCharacter __instance) => pendingCells.Add(__instance);
        }

        [HarmonyPatch(typeof(HudGroupCharacterView), nameof(HudGroupCharacterView.Bind))]
        static class HudGroupCharacterViewBindPatch {
            static void Postfix(HudGroupCharacterView __instance) => pendingViews.Add(__instance);
        }

        static void EnsureBadge(Transform cell, RectTransform portraitImage, Func<UnitEntityData> unit) {
            if (cell.Find(BadgeName) != null) return;

            var (go, rect) = UIHelpers.Create(BadgeName, cell);
            rect.SetAnchor(0, 0, 1, 1); // point-anchor at the cell's top-left corner (fallback
                                        // position until PortraitToggleBadge aligns it to the image)
            // 26 px: the 22 px Wrath size read too small on Kingmaker's ~110 px cells (deck
            // screenshot 2026-10-03) and is hard to hit with the Steam Deck trackpad. The image's
            // top-left is free: status/buff icons sit in the column right of the image.
            float size = 26f * UIHelpers.FontScale;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(size * 0.5f + 2f, -(size * 0.5f + 2f));
            go.transform.SetAsLastSibling(); // draw above the portrait frame

            UIHelpers.AddBackground(go, Theme.DimBackdrop);
            var label = Widgets.BandLabel(go, "T", 18f, TextAlignmentOptions.Midline);

            var badge = go.AddComponent<PortraitToggleBadge>();
            badge.Init(unit, label, portraitImage);
            // A child Button is the nearest click handler, so the portrait's own
            // OnPointerClick (select unit / cast onto portrait) does not fire for this click.
            go.AddComponent<Button>().onClick.AddListener(badge.OnClick);

            badges.Add(badge);
        }
    }
}
