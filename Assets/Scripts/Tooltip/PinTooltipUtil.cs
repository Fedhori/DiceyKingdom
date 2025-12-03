using System.Collections.Generic;
using System.Text;
using Data;
using GameStats;
using UnityEngine;
using UnityEngine.Localization;

public static class PinTooltipUtil
{
    public static TooltipModel BuildModel(PinInstance pin)
    {
        if (pin == null || pin.BaseDto == null)
        {
            return new TooltipModel
            (
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Pin
            );
        }

        var title = LocalizationUtil.GetPinName(pin.Id);
        var body = BuildBody(pin);
        var icon = SpriteCache.GetPinSprite(pin.Id);

        return new TooltipModel
        (
            title,
            body,
            icon,
            TooltipKind.Pin
        );
    }

    static string BuildBody(PinInstance pin)
    {
        var lines = new List<string>();

        AppendBasePassiveLines(pin, lines);
        AppendRuleLines(pin, lines);

        if (lines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
                sb.Append('\n');
            sb.Append(lines[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// PinDto.scoreMultiplier 같은 "룰이 아닌 패시브" 한 줄.
    /// 예) scoreMultiplier != 1 인 경우: "점수 x3"
    /// </summary>
    static void AppendBasePassiveLines(PinInstance pin, List<string> lines)
    {
        var dto = pin.BaseDto;
        if (dto == null)
            return;

        if (dto.scoreMultiplier > 1.0001f || dto.scoreMultiplier < 0.9999f)
        {
            // table: pin, key: effect.base.scoreMultiplier
            // ko: "점수 x{value}"
            var loc = new LocalizedString("pin", "effect.base.scoreMultiplier");
            loc.Arguments = new object[]
            {
                new { value = dto.scoreMultiplier.ToString("0.##") }
            };
            var text = loc.GetLocalizedString();

            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }
    }

    static void AppendRuleLines(PinInstance pin, List<string> lines)
    {
        var rules = pin.Rules;
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.effects == null || rule.effects.Count == 0)
                continue;

            var line = BuildRuleLine(pin, rule);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    static string BuildRuleLine(PinInstance pin, PinRuleDto rule)
    {
        // 1) 효과 문장 먼저 생성
        string effectsText = BuildEffectsText(pin, rule.effects);
        if (string.IsNullOrEmpty(effectsText))
            return string.Empty;

        // 2) Charge 조건 여부 확인
        PinConditionDto chargeCond = null;
        bool hasNonChargeNonAlwaysCondition = false;

        if (rule.conditions != null)
        {
            for (int i = 0; i < rule.conditions.Count; i++)
            {
                var cond = rule.conditions[i];
                if (cond == null)
                    continue;

                if (cond.conditionKind == PinConditionKind.Charge)
                {
                    if (chargeCond == null)
                        chargeCond = cond;
                }
                else if (cond.conditionKind != PinConditionKind.Always)
                {
                    hasNonChargeNonAlwaysCondition = true;
                }
            }
        }

        string prefix;

        // 3) Charge 단독(또는 Always와만 섞인) + Trigger=OnBallHit → "충전 N"만 사용
        if (chargeCond != null &&
            !hasNonChargeNonAlwaysCondition &&
            rule.triggerType == PinTriggerType.OnBallHit)
        {
            prefix = GetChargePrefix(chargeCond);
        }
        else
        {
            // 그 외에는 Trigger + Condition 텍스트 결합
            var triggerText = GetTriggerText(rule.triggerType);
            var conditionText = GetConditionsText(rule.conditions);

            if (string.IsNullOrEmpty(triggerText))
                prefix = conditionText;
            else if (string.IsNullOrEmpty(conditionText))
                prefix = triggerText;
            else
                prefix = $"{triggerText}, {conditionText}";
        }

        if (string.IsNullOrEmpty(prefix))
            return effectsText;

        return $"{prefix}: {effectsText}";
    }

    static string GetTriggerText(PinTriggerType triggerType)
    {
        switch (triggerType)
        {
            case PinTriggerType.OnBallHit:
            {
                // table: pin, key: trigger.onBallHit
                // ko: "볼과 충돌할 때마다"
                var loc = new LocalizedString("pin", "trigger.onBallHit");
                return loc.GetLocalizedString();
            }
            case PinTriggerType.OnBallDestroyed:
            {
                // table: pin, key: trigger.onBallDestroyed
                // ko: "볼이 파괴될 때마다"
                var loc = new LocalizedString("pin", "trigger.onBallDestroyed");
                return loc.GetLocalizedString();
            }
            default:
                return string.Empty;
        }
    }

    static string GetConditionsText(List<PinConditionDto> conditions)
    {
        if (conditions == null || conditions.Count == 0)
            return string.Empty;

        var list = new List<string>();

        for (int i = 0; i < conditions.Count; i++)
        {
            var cond = conditions[i];
            if (cond == null)
                continue;

            switch (cond.conditionKind)
            {
                case PinConditionKind.Always:
                    // "항상"은 생략
                    break;

                case PinConditionKind.Charge:
                    // 미래에 Trigger랑 같이 보여줄 때 사용
                    list.Add(GetChargeWhenText(cond));
                    break;
            }
        }

        if (list.Count == 0)
            return string.Empty;

        return string.Join(", ", list);
    }

    /// <summary>
    /// Charge 전용 "충전 N" 프리픽스 (Trigger 생략 버전).
    /// </summary>
    static string GetChargePrefix(PinConditionDto cond)
    {
        if (cond == null || cond.hits <= 0)
            return string.Empty;

        // table: pin, key: condition.charge.prefix
        // ko: "충전 {hits}"
        var loc = new LocalizedString("pin", "condition.charge.prefix");
        loc.Arguments = new object[]
        {
            new { hits = cond.hits }
        };
        return loc.GetLocalizedString();
    }

    /// <summary>
    /// Trigger와 함께 쓸 때의 Charge 문구. (현재는 안씀, 확장용)
    /// </summary>
    static string GetChargeWhenText(PinConditionDto cond)
    {
        if (cond == null || cond.hits <= 0)
            return string.Empty;

        // table: pin, key: condition.charge.when
        // ko: "충전 {hits}일 때"
        var loc = new LocalizedString("pin", "condition.charge.when");
        loc.Arguments = new object[]
        {
            new { hits = cond.hits }
        };
        return loc.GetLocalizedString();
    }

    static string BuildEffectsText(PinInstance pin, List<PinEffectDto> effects)
    {
        if (effects == null || effects.Count == 0)
            return string.Empty;

        var parts = new List<string>();

        for (int i = 0; i < effects.Count; i++)
        {
            var e = effects[i];
            if (e == null)
                continue;

            var text = BuildSingleEffectText(pin, e);
            if (!string.IsNullOrEmpty(text))
                parts.Add(text);
        }

        if (parts.Count == 0)
            return string.Empty;

        return string.Join(" / ", parts);
    }

    static string BuildSingleEffectText(PinInstance pin, PinEffectDto effect)
    {
        switch (effect.effectType)
        {
            case PinEffectType.ModifyPlayerStat:
                return BuildModifyStatText(effect, isSelf: false);

            case PinEffectType.ModifySelfStat:
                return BuildModifyStatText(effect, isSelf: true);

            case PinEffectType.AddVelocity:
                return BuildAddVelocityText(effect);

            case PinEffectType.IncreaseSize:
                return BuildIncreaseSizeText(effect);

            case PinEffectType.AddScore:
                return BuildAddScoreText(effect);

            default:
                return string.Empty;
        }
    }

    static string BuildModifyStatText(PinEffectDto effect, bool isSelf)
    {
        if (string.IsNullOrEmpty(effect.statId))
            return string.Empty;

        string statName = GetStatDisplayName(effect.statId, isSelf);
        if (string.IsNullOrEmpty(statName))
            statName = effect.statId;

        string valueText = FormatStatDelta(effect.statId, effect.effectMode, effect.value);
        if (string.IsNullOrEmpty(valueText))
            return statName;

        return $"{statName} {valueText}";
    }

    static string FormatStatDelta(string statId, StatOpKind opKind, float value)
    {
        switch (statId)
        {
            case PlayerStatIds.Score:
            {
                int v = Mathf.RoundToInt(value);
                if (v == 0) return string.Empty;
                return v > 0 ? $"+{v}" : v.ToString();
            }

            case PinStatIds.ScoreMultiplier:
            {
                switch (opKind)
                {
                    case StatOpKind.Add:
                        if (Mathf.Approximately(value, 0f)) return string.Empty;
                        return value > 0f
                            ? $"+{value:0.##}"
                            : value.ToString("0.##");
                    case StatOpKind.Mult:
                        if (Mathf.Approximately(value, 1f)) return string.Empty;
                        return $"x{value:0.##}";
                    default:
                        return string.Empty;
                }
            }

            case PlayerStatIds.CriticalChance:
            {
                if (Mathf.Approximately(value, 0f)) return string.Empty;
                float percent = value;
                string core = percent > 0f
                    ? $"+{percent:0.##}"
                    : percent.ToString("0.##");
                return $"{core}%";
            }

            case PlayerStatIds.CriticalMultiplier:
            {
                switch (opKind)
                {
                    case StatOpKind.Add:
                        if (Mathf.Approximately(value, 0f)) return string.Empty;
                        return value > 0f
                            ? $"+{value:0.##}"
                            : value.ToString("0.##");
                    case StatOpKind.Mult:
                        if (Mathf.Approximately(value, 1f)) return string.Empty;
                        return $"x{value:0.##}";
                    default:
                        return string.Empty;
                }
            }

            default:
            {
                switch (opKind)
                {
                    case StatOpKind.Add:
                        if (Mathf.Approximately(value, 0f)) return string.Empty;
                        return value > 0f
                            ? $"+{value:0.##}"
                            : value.ToString("0.##");
                    case StatOpKind.Mult:
                        if (Mathf.Approximately(value, 1f)) return string.Empty;
                        return $"x{value:0.##}";
                    default:
                        return string.Empty;
                }
            }
        }
    }

    static string BuildAddVelocityText(PinEffectDto effect)
    {
        if (Mathf.Approximately(effect.value, 1f))
            return string.Empty;

        var factor = effect.value;

        // table: pin, key: effect.addVelocity
        // ko: "볼 속도 x{value}"
        var loc = new LocalizedString("pin", "effect.addVelocity");
        loc.Arguments = new object[]
        {
            new { value = factor.ToString("0.##") }
        };
        return loc.GetLocalizedString();
    }

    static string BuildIncreaseSizeText(PinEffectDto effect)
    {
        if (Mathf.Approximately(effect.value, 1f))
            return string.Empty;

        var factor = effect.value;

        // table: pin, key: effect.increaseSize
        // ko: "볼 크기 x{value}"
        var loc = new LocalizedString("pin", "effect.increaseSize");
        loc.Arguments = new object[]
        {
            new { value = factor.ToString("0.##") }
        };
        return loc.GetLocalizedString();
    }

    static string BuildAddScoreText(PinEffectDto effect)
    {
        int v = Mathf.RoundToInt(effect.value);
        if (v == 0)
            return string.Empty;

        // table: pin, key: effect.addScore
        // ko: "+{value}점"
        var loc = new LocalizedString("pin", "effect.addScore");
        loc.Arguments = new object[]
        {
            new { value = v }
        };
        return loc.GetLocalizedString();
    }

    static string GetStatDisplayName(string statId, bool isSelf)
    {
        if (string.IsNullOrEmpty(statId))
            return string.Empty;

        string prefix = isSelf ? "pin" : "player";
        string key = $"{prefix}.{statId}";

        var loc = new LocalizedString("stat", key);
        return loc.GetLocalizedString();
    }
}
