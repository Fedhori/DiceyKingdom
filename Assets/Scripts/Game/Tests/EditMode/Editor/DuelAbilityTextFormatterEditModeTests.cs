using System.Collections.Generic;
using System.Reflection;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
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

            AbilityDef def = CreateAbilityDef(
                "ability.regeneration",
                new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyHealth",
                                value = 1
                            }
                        }
                    }
                });

            var ability = new AbilityInstance
            {
                abilityDefId = def.id,
                abilityType = AbilityType.Passive,
                power = 0,
                cooldownTurns = 0,
                cooldownRemaining = 0
            };

            string tooltip = formatter.FormatTooltip(def, ability);

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

            AbilityDef def = CreateAbilityDef(
                "ability.multi",
                new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyHealth",
                                value = 2
                            }
                        }
                    },
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyHealth",
                                value = 3
                            }
                        }
                    }
                });

            string tooltip = formatter.FormatTooltip(def, ability: null);

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

            AbilityDef def = CreateAbilityDef(
                "ability.multi",
                new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyHealth",
                                value = 2
                            }
                        }
                    },
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyHealth",
                                value = 3
                            }
                        }
                    }
                });

            string tooltip = formatter.FormatTooltip(def, ability: null);

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

            AbilityDef def = CreateAbilityDef("ability.empty", effects: new List<TimedEffectDef>());

            string tooltip = formatter.FormatTooltip(def, ability: null);

            Assert.AreEqual("빈 능력", tooltip);
        }

        static AbilityDef CreateAbilityDef(string id, List<TimedEffectDef> effects)
        {
            return new AbilityDef
            {
                type = AbilityType.Passive.ToString(),
                buildCost = 0,
                cooldown = 0,
                power = 0,
                nameLocKey = $"{id}.name",
                descLocKey = $"{id}.desc",
                isPlayerObtainable = true,
                iconId = "icon.default",
                effects = effects ?? new List<TimedEffectDef>()
            };
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
