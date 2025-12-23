using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class TokenInstance
{
    private TokenDto BaseDto { get; }

    public string Id => BaseDto.id;
    public TokenRarity Rarity => BaseDto.rarity;
    public int Price => BaseDto.price;

    private readonly List<TokenRuleDto> rules;
    public IReadOnlyList<TokenRuleDto> Rules => rules;

    public TokenInstance(TokenDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        if (dto.rules != null)
            rules = new List<TokenRuleDto>(dto.rules);
        else
            rules = new List<TokenRuleDto>();
    }

    public void HandleTrigger(TokenTriggerType trigger)
    {
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.triggerType != trigger)
                continue;

            if (!IsConditionMet(rule.condition, trigger))
                continue;

            ApplyEffects(rule.effects);
        }
    }

    bool IsConditionMet(TokenConditionDto cond, TokenTriggerType trigger)
    {
        if (cond == null)
            return false;

        switch (cond.conditionKind)
        {
            case TokenConditionKind.Always:
                return true;
            default:
                Debug.LogWarning($"[TokenInstance] Unsupported condition {cond.conditionKind} for trigger {trigger}");
                return false;
        }
    }

    void ApplyEffects(List<TokenEffectDto> effects)
    {
        if (effects == null || effects.Count == 0)
            return;

        var effectMgr = TokenEffectManager.Instance;
        if (effectMgr == null)
        {
            Debug.LogWarning("[TokenInstance] TokenEffectManager is null.");
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            effectMgr.ApplyEffect(effect, this);
        }
    }
}
