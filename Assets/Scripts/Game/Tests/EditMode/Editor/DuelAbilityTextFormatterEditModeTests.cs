using System.Collections.Generic;
using System.Reflection;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Presentation.Duel;
using Game.Presentation.Localization;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelAbilityTextFormatterEditModeTests
    {
        [Test]
        public void FormatTooltip_SingleEffect_InterpolatesAmount()
        {
            var resolver = new FakeLocalizedTextResolver(new Dictionary<string, string>
            {
                ["ability:ability.regeneration.name"] = "재생력",
                ["ability:ability.regeneration.desc"] = "턴 종료 시 체력 +{0.amount}"
            });
            var formatter = new DuelAbilityTextFormatter(resolver);

            DuelUiAbilityData abilityData = CreateAbilityData(
                "ability.regeneration",
                DuelUiAbilityType.Passive,
                0,
                0,
                0,
                new DuelUiEffectLineData(1, "ModifyHealth"));

            string tooltip = formatter.FormatTooltip(abilityData);

            Assert.AreEqual("재생력\n턴 종료 시 체력 +1", tooltip);
        }

        [Test]
        public void FormatTooltip_MultiEffect_UsesDescAndDescDotIndex()
        {
            var resolver = new FakeLocalizedTextResolver(new Dictionary<string, string>
            {
                ["ability:ability.multi.name"] = "복합 능력",
                ["ability:ability.multi.desc"] = "효과1 +{0.amount}",
                ["ability:ability.multi.desc.2"] = "효과2 +{0.amount}"
            });
            var formatter = new DuelAbilityTextFormatter(resolver);

            DuelUiAbilityData abilityData = CreateAbilityData(
                "ability.multi",
                DuelUiAbilityType.Passive,
                0,
                0,
                0,
                new DuelUiEffectLineData(2, "ModifyHealth"),
                new DuelUiEffectLineData(3, "ModifyHealth"));

            string tooltip = formatter.FormatTooltip(abilityData);

            Assert.AreEqual("복합 능력\n효과1 +2\n효과2 +3", tooltip);
        }

        [Test]
        public void FormatTooltip_MissingDescDotIndex_IsIgnored()
        {
            var resolver = new FakeLocalizedTextResolver(new Dictionary<string, string>
            {
                ["ability:ability.multi.name"] = "복합 능력",
                ["ability:ability.multi.desc"] = "효과1 +{0.amount}"
            });
            var formatter = new DuelAbilityTextFormatter(resolver);

            DuelUiAbilityData abilityData = CreateAbilityData(
                "ability.multi",
                DuelUiAbilityType.Passive,
                0,
                0,
                0,
                new DuelUiEffectLineData(2, "ModifyHealth"),
                new DuelUiEffectLineData(3, "ModifyHealth"));

            string tooltip = formatter.FormatTooltip(abilityData);

            Assert.AreEqual("복합 능력\n효과1 +2", tooltip);
        }

        [Test]
        public void FormatTooltip_NoEffects_MissingDesc_IsNotAnErrorPath()
        {
            var resolver = new FakeLocalizedTextResolver(new Dictionary<string, string>
            {
                ["ability:ability.empty.name"] = "빈 능력"
            });
            var formatter = new DuelAbilityTextFormatter(resolver);

            DuelUiAbilityData abilityData = CreateAbilityData(
                "ability.empty",
                DuelUiAbilityType.Passive,
                0,
                0,
                0);

            string tooltip = formatter.FormatTooltip(abilityData);

            Assert.AreEqual("빈 능력", tooltip);
        }

        static DuelUiAbilityData CreateAbilityData(
            string id,
            DuelUiAbilityType abilityType,
            int power,
            int cooldownTurns,
            int cooldownRemaining,
            params DuelUiEffectLineData[] effects)
        {
            IReadOnlyList<DuelUiEffectLineData> effectLines = effects == null
                ? new List<DuelUiEffectLineData>()
                : new List<DuelUiEffectLineData>(effects);
            return new DuelUiAbilityData(
                id,
                id,
                abilityType,
                power,
                cooldownTurns,
                cooldownRemaining,
                $"{id}.name",
                $"{id}.desc",
                "icon.default",
                "Data/icons/icon.default.png",
                effectLines);
        }

        sealed class FakeLocalizedTextResolver : ILocalizedTextResolver
        {
            readonly Dictionary<string, string> entriesByKey;

            public FakeLocalizedTextResolver(Dictionary<string, string> entriesByKey)
            {
                this.entriesByKey = entriesByKey ?? new Dictionary<string, string>();
            }

            public string ResolveRequired(string tableName, string key, object arguments = null)
            {
                if (!entriesByKey.TryGetValue($"{tableName}:{key}", out string template))
                {
                    return $"[missing:{key}]";
                }

                return ReplaceArguments(template, arguments);
            }

            public string ResolveOptional(string tableName, string key, object arguments = null, bool warnIfMissing = false)
            {
                if (!entriesByKey.TryGetValue($"{tableName}:{key}", out string template))
                {
                    return string.Empty;
                }

                return ReplaceArguments(template, arguments);
            }

            static string ReplaceArguments(string template, object arguments)
            {
                if (arguments == null)
                {
                    return template;
                }

                string resolved = template;
                PropertyInfo[] properties = arguments.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo property = properties[i];
                    object value = property.GetValue(arguments);
                    resolved = resolved.Replace($"{{0.{property.Name}}}", value?.ToString() ?? string.Empty);
                }

                return resolved;
            }
        }
    }
}
