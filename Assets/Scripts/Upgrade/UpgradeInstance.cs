using System;
using System.Collections.Generic;
using Data;

public sealed class UpgradeInstance
{
    public string Id { get; }
    public string UniqueId { get; }
    public int Price { get; }
    public ItemRarity Rarity { get; }
    public bool RequiresSolo { get; }
    public float BreakChanceOnStageEnd { get; }
    public IReadOnlyList<UpgradeConditionDto> Conditions => conditions;
    public IReadOnlyList<ItemEffectDto> Effects => effects;
    public IReadOnlyList<ItemRuleDto> Rules => rules;

    readonly List<UpgradeConditionDto> conditions = new();
    readonly List<ItemEffectDto> effects = new();
    readonly List<ItemRuleDto> rules = new();

    public UpgradeInstance(UpgradeDto dto)
    {
        if (dto == null)
        {
            Id = string.Empty;
            Price = 0;
            Rarity = ItemRarity.Common;
            return;
        }

        Id = dto.id;
        UniqueId = Guid.NewGuid().ToString();
        Price = dto.price;
        Rarity = dto.rarity;
        RequiresSolo = dto.requiresSolo;
        BreakChanceOnStageEnd = dto.breakChanceOnStageEnd;

        if (dto.conditions != null)
            conditions.AddRange(dto.conditions);

        if (dto.effects != null)
            effects.AddRange(dto.effects);

        if (dto.rules != null)
            rules.AddRange(dto.rules);
    }

    public bool IsApplicable(ItemInstance target)
    {
        if (target == null)
            return false;

        if (conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            if (condition == null)
                continue;

            if (!UpgradeConditionEvaluator.IsSatisfied(condition, target))
                return false;
        }

        return true;
    }
}
