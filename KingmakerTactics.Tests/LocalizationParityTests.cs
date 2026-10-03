using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.UnitLogic.Abilities;
using KingmakerTactics.Localization;
using KingmakerTactics.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KingmakerTactics.Tests {
    // Enum labels are built at runtime ($"enum.action.{v}"), so no static grep catches a
    // member added without its key — enum.action.MoveToTarget went missing in fr/ru/zh.
    public class LocalizationParityTests {
        static readonly string[] Locales = { "en_GB", "de_DE", "fr_FR", "ru_RU", "zh_CN" };

        static HashSet<string> Keys(string locale) {
            var asm = typeof(EnumLabels).Assembly;
            using (var stream = asm.GetManifestResourceStream($"KingmakerTactics.Localization.{locale}.json"))
            using (var reader = new StreamReader(stream)) {
                return new HashSet<string>(JObject.Parse(reader.ReadToEnd()).Properties().Select(p => p.Name));
            }
        }

        static IEnumerable<string> EnumKeys<T>(string typeKey, params T[] skip) where T : Enum =>
            ((T[])Enum.GetValues(typeof(T))).Where(v => !skip.Contains(v)).Select(v => $"enum.{typeKey}.{v}");

        static IEnumerable<string> ExpectedEnumKeys() =>
            EnumKeys<ConditionSubject>("subject")
                .Concat(EnumKeys<ConditionProperty>("property"))
                .Concat(EnumKeys<ActionType>("action"))
                .Concat(EnumKeys<TargetType>("target"))
                .Concat(EnumKeys<HealMode>("heal_mode"))
                .Concat(EnumKeys<ToggleMode>("toggle_mode"))
                .Concat(EnumKeys<ThrowSplashMode>("splash_mode"))
                .Concat(EnumKeys<RangeBracket>("range"))
                .Concat(EnumKeys<HealEnergyType>("heal_energy", HealEnergyType.None)) // sentinel, never shown
                .Concat(EnumLabels.KeysForCreatureType().Select(k => $"enum.creature_type.{k}"))
                .Concat(EnumLabels.KeysForAlignment().Select(k => $"enum.alignment.{k}"))
                .Concat(EnumLabels.KeysForCondition().Select(k => $"enum.condition.{k}"))
                .Concat(EnumLabels.KeysForDescriptorEffect().Select(k => $"enum.descriptor.{k}"))
                .Concat(EnumLabels.KeysForEnergy().Select(k => $"enum.energy.{k}"))
                .Concat(EnumLabels.MetamagicValues.Select(v => $"enum.metamagic.{v}"));

        [Fact]
        public void Every_enum_label_exists_in_every_locale() {
            foreach (var locale in Locales) {
                var keys = Keys(locale);
                var missing = ExpectedEnumKeys().Where(k => !keys.Contains(k)).ToList();
                Assert.True(missing.Count == 0, $"{locale} missing: {string.Join(", ", missing)}");
            }
        }

        [Fact]
        public void All_locales_carry_the_same_keys_as_en_GB() {
            var en = Keys("en_GB");
            foreach (var locale in Locales.Skip(1)) {
                var keys = Keys(locale);
                var missing = en.Where(k => !keys.Contains(k)).ToList();
                var extra = keys.Where(k => !en.Contains(k)).ToList();
                Assert.True(missing.Count == 0 && extra.Count == 0,
                    $"{locale} missing: {string.Join(", ", missing)}; extra: {string.Join(", ", extra)}");
            }
        }
    }
}
