using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Presentation.Localization;
using UnityEngine;

namespace Game.Presentation.Duel
{
    public sealed class DuelAbilityTextFormatter
    {
        const string abilityTableName = "ability";

        readonly ILocalizedTextResolver localizedTextResolver;

        public DuelAbilityTextFormatter(ILocalizedTextResolver localizedTextResolver)
        {
            this.localizedTextResolver = localizedTextResolver ?? throw new ArgumentNullException(nameof(localizedTextResolver));
        }

        public string FormatName(AbilityDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            return localizedTextResolver.Resolve(abilityTableName, def.nameLocKey);
        }

        public string FormatTooltip(AbilityDef def, AbilityInstance ability)
        {
            if (def == null)
            {
                return string.Empty;
            }

            string localizedName = FormatName(def);
            string localizedDescription = FormatDescription(def, ability);
            if (string.IsNullOrWhiteSpace(localizedDescription))
            {
                return localizedName;
            }

            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return localizedDescription;
            }

            return $"{localizedName}\n{localizedDescription}";
        }

        string FormatDescription(AbilityDef def, AbilityInstance ability)
        {
            if (def == null)
            {
                return string.Empty;
            }

            List<TimedEffectDef> effects = def.effects;
            if (effects == null || effects.Count <= 0)
            {
                AbilityLocArgs args = BuildArgs(def, ability, 0);
                return localizedTextResolver.Resolve(abilityTableName, def.descLocKey, args);
            }

            var lines = new List<string>(effects.Count);
            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                string key = effectIndex == 0
                    ? def.descLocKey
                    : $"{def.descLocKey}.{effectIndex + 1}";
                AbilityLocArgs args = BuildArgs(def, ability, effectIndex);
                string line = localizedTextResolver.Resolve(abilityTableName, key, args);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                lines.Add(line);
            }

            return string.Join("\n", lines);
        }

        static AbilityLocArgs BuildArgs(AbilityDef def, AbilityInstance ability, int effectIndex)
        {
            int power = ability == null
                ? Mathf.Max(0, def.ResolvePower())
                : Mathf.Max(0, ability.power);
            int cooldown = ResolveCooldownTurns(def, ability);
            int cooldownRemaining = ability == null
                ? 0
                : Mathf.Max(0, ability.cooldownRemaining);
            int amount = ResolveAmount(def, effectIndex);
            string op = ResolveOp(def, effectIndex);

            return new AbilityLocArgs(
                amount,
                power,
                cooldown,
                cooldownRemaining,
                op,
                effectIndex + 1);
        }

        static int ResolveCooldownTurns(AbilityDef def, AbilityInstance ability)
        {
            if (ability != null)
            {
                return Mathf.Max(0, ability.cooldownTurns);
            }

            AbilityType abilityType = AbilityType.Attack;
            if (!def.TryGetAbilityType(out abilityType))
            {
                abilityType = AbilityType.Attack;
            }

            return Mathf.Max(0, def.ResolveCooldownTurns(abilityType));
        }

        static int ResolveAmount(AbilityDef def, int effectIndex)
        {
            if (def?.effects == null || effectIndex < 0 || effectIndex >= def.effects.Count)
            {
                return 0;
            }

            TimedEffectDef effect = def.effects[effectIndex];
            if (effect?.ops == null || effect.ops.Count <= 0)
            {
                return 0;
            }

            for (int opIndex = 0; opIndex < effect.ops.Count; opIndex++)
            {
                EffectOpDef opDef = effect.ops[opIndex];
                if (opDef != null && opDef.TryGetAmount(out int amount))
                {
                    return amount;
                }
            }

            return 0;
        }

        static string ResolveOp(AbilityDef def, int effectIndex)
        {
            if (def?.effects == null || effectIndex < 0 || effectIndex >= def.effects.Count)
            {
                return string.Empty;
            }

            TimedEffectDef effect = def.effects[effectIndex];
            if (effect?.ops == null || effect.ops.Count <= 0)
            {
                return string.Empty;
            }

            for (int opIndex = 0; opIndex < effect.ops.Count; opIndex++)
            {
                EffectOpDef opDef = effect.ops[opIndex];
                if (opDef != null && !string.IsNullOrWhiteSpace(opDef.op))
                {
                    return opDef.op;
                }
            }

            return string.Empty;
        }

        sealed class AbilityLocArgs
        {
            public int amount { get; }
            public int power { get; }
            public int cooldown { get; }
            public int cooldownRemaining { get; }
            public string op { get; }
            public int effectIndex { get; }

            public AbilityLocArgs(
                int amount,
                int power,
                int cooldown,
                int cooldownRemaining,
                string op,
                int effectIndex)
            {
                this.amount = amount;
                this.power = power;
                this.cooldown = cooldown;
                this.cooldownRemaining = cooldownRemaining;
                this.op = op ?? string.Empty;
                this.effectIndex = effectIndex;
            }
        }
    }
}
