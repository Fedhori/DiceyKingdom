using System;
using Game.Application.Duel;
using Game.Presentation.Localization;
using System.Collections.Generic;

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

        public string FormatName(DuelUiAbilityData abilityData)
        {
            if (string.IsNullOrWhiteSpace(abilityData.nameLocKey))
            {
                return string.Empty;
            }

            return localizedTextResolver.ResolveRequired(abilityTableName, abilityData.nameLocKey);
        }

        public string FormatTooltip(DuelUiAbilityData abilityData)
        {
            string localizedName = FormatName(abilityData);
            string localizedDescription = FormatDescription(abilityData);
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

        public string FormatDescription(DuelUiAbilityData abilityData)
        {
            if (string.IsNullOrWhiteSpace(abilityData.descLocKey))
            {
                return string.Empty;
            }

            if (!abilityData.hasEffects)
            {
                return localizedTextResolver.ResolveOptional(
                    abilityTableName,
                    abilityData.descLocKey,
                    arguments: null,
                    warnIfMissing: false);
            }

            var lines = new List<string>(abilityData.effects.Count);
            for (int effectIndex = 0; effectIndex < abilityData.effects.Count; effectIndex++)
            {
                string key = effectIndex == 0
                    ? abilityData.descLocKey
                    : $"{abilityData.descLocKey}.{effectIndex + 1}";
                AbilityLocArgs args = BuildArgs(abilityData, effectIndex);
                bool warnIfMissing = effectIndex == 0;
                string line = localizedTextResolver.ResolveOptional(abilityTableName, key, args, warnIfMissing);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                lines.Add(line);
            }

            return string.Join("\n", lines);
        }

        static AbilityLocArgs BuildArgs(DuelUiAbilityData abilityData, int effectIndex)
        {
            int amount = 0;
            string op = string.Empty;
            if (abilityData.effects != null && effectIndex >= 0 && effectIndex < abilityData.effects.Count)
            {
                DuelUiEffectLineData effectLine = abilityData.effects[effectIndex];
                amount = effectLine.amount;
                op = effectLine.op;
            }

            return new AbilityLocArgs(
                amount,
                abilityData.power,
                abilityData.cooldownTurns,
                abilityData.cooldownRemaining,
                op,
                effectIndex + 1);
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
