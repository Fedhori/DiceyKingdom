using System;
using System.Collections.Generic;

public sealed class MissionEffectSummaryBuilder
{
    readonly HashSet<string> missingEffectLogged = new(StringComparer.Ordinal);

    public string BuildSuccessSummary(MissionDef missionDef)
    {
        string body = BuildSummary(
            missionDef,
            RuleTriggerIds.OnExpeditionResolved,
            RuleConditionIds.ExpeditionSucceeded);
        return $"성공: {body}";
    }

    public string BuildDeadlineFailSummary(MissionDef missionDef)
    {
        string body = BuildSummary(
            missionDef,
            RuleTriggerIds.OnMissionFailed,
            null);
        return $"기한 실패: {body}";
    }

    string BuildSummary(MissionDef missionDef, string trigger, string conditionId)
    {
        if (missionDef?.rules == null || missionDef.rules.Count == 0)
            return "없음";

        var tokens = new List<string>();
        for (int i = 0; i < missionDef.rules.Count; i++)
        {
            RuleDef rule = missionDef.rules[i];
            if (rule == null)
                continue;

            if (!string.Equals(rule.trigger, trigger, StringComparison.Ordinal))
                continue;

            if (!string.IsNullOrWhiteSpace(conditionId))
            {
                string ruleConditionId = rule.condition?.conditionId ?? string.Empty;
                if (!string.Equals(ruleConditionId, conditionId, StringComparison.Ordinal))
                    continue;
            }

            if (rule.effects == null)
                continue;

            for (int effectIndex = 0; effectIndex < rule.effects.Count; effectIndex++)
            {
                EffectDef effect = rule.effects[effectIndex];
                if (effect == null)
                    continue;

                string token = ToDisplayToken(effect);
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                tokens.Add(token);
            }
        }

        if (tokens.Count == 0)
            return "없음";

        return string.Join(" · ", tokens);
    }

    string ToDisplayToken(EffectDef effect)
    {
        switch (effect.effectId)
        {
            case EffectIds.AddGold:
                return $"골드 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddStability:
                return $"안정도 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddHpSelf:
                return $"자신 체력 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddHpAssignedParty:
                return $"배치 인원 체력 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddHpAllAdventurers:
                return $"전체 모험가 체력 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddStaminaSelf:
                return $"자신 기력 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddStaminaAssignedParty:
                return $"배치 인원 기력 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddStaminaAllAdventurers:
                return $"전체 모험가 기력 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddXpSelf:
                return $"자신 경험치 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddXpAssignedParty:
                return $"배치 인원 경험치 {FormatSigned(ReadInt(effect.@params, 0))}";
            case EffectIds.AddAbilitySelf:
                return BuildAbilityToken("자신", effect.@params);
            case EffectIds.AddAbilityAssignedParty:
                return BuildAbilityToken("배치 인원", effect.@params);
            case EffectIds.AddAbilityAllAdventurers:
                return BuildAbilityToken("전체 모험가", effect.@params);
            default:
                if (missingEffectLogged.Add(effect.effectId ?? string.Empty))
                    UnityEngine.Debug.LogError($"[MissionOverlay] Unsupported effectId for summary: {effect.effectId}");
                return "알 수 없는 효과";
        }
    }

    static string BuildAbilityToken(string subject, IReadOnlyList<float> values)
    {
        int strength = ReadInt(values, 0);
        int agility = ReadInt(values, 1);
        int intelligence = ReadInt(values, 2);
        var segments = new List<string>();
        if (strength != 0)
            segments.Add($"힘 {FormatSigned(strength)}");
        if (agility != 0)
            segments.Add($"민첩 {FormatSigned(agility)}");
        if (intelligence != 0)
            segments.Add($"지능 {FormatSigned(intelligence)}");

        if (segments.Count == 0)
            return $"{subject} 능력치 변화 없음";

        return $"{subject} {string.Join("/", segments)}";
    }

    static int ReadInt(IReadOnlyList<float> values, int index)
    {
        if (values == null || index < 0 || index >= values.Count)
            return 0;

        return EffectMath.FloorToInt(values[index]);
    }

    static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }
}
