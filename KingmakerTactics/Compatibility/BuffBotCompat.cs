using System;
using System.Reflection;
using HarmonyLib;
using KingmakerTactics.Logging;
using UnityModManagerNet;

namespace KingmakerTactics.Compatibility {
    /// <summary>
    /// Buff Bot (Balkoth, UMM Id "KingmakerBuffBot") casts via AbilityData.Cast directly: no
    /// UnitCommand, no cast time, no HUD element, no Harmony patches — nothing to coordinate.
    /// The one overlap is its optional "cast buffs on combat start": our missing-buff rules
    /// fire on the same transition and spend the slot a second time. Detection is log-only so
    /// a "spells cast twice" report can be triaged from the log.
    /// </summary>
    public static class BuffBotCompat {
        const string ModId = "KingmakerBuffBot";
        const string MainType = "KingmakerBuffBot.Main";
        static bool logged;

        /// <summary>Called on every combat start; logs once per session (Buff Bot's settings
        /// are only populated after its own Load, so Main.Load is too early).</summary>
        public static void LogOnce() {
            if (logged) return;
            logged = true;
            try {
                var entry = UnityModManager.FindMod(ModId);
                if (entry == null || !entry.Active) return;
                var castOnStart = ReadCastCombatStart(AccessTools.TypeByName(MainType));
                Log.Compat.Info($"Buff Bot detected (castCombatStart={(castOnStart.HasValue ? castOnStart.Value.ToString() : "unknown")})");
                if (castOnStart == true)
                    Log.Compat.Warn("Buff Bot casts buffs on combat start: tactics rules casting the same buffs at combat start spend the slot twice");
            } catch (Exception e) {
                Log.Compat.Warn($"Buff Bot detection failed: {e.Message}");
            }
        }

        /// <summary>Main.settings.castCombatStart via reflection; null when the type, the
        /// static field or its value is missing (layout change, or Buff Bot not loaded yet).</summary>
        public static bool? ReadCastCombatStart(Type main) {
            var settingsField = main?.GetField("settings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var settings = settingsField?.GetValue(null);
            if (settings == null) return null;
            var flag = settings.GetType().GetField("castCombatStart", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return flag?.GetValue(settings) as bool?;
        }
    }
}
