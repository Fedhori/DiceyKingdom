using System;
using System.Collections.Generic;
using Data;

public sealed class UpgradeInstance
{
    public string Id { get; }
    public int Price { get; }
    public IReadOnlyList<UpgradeConditionDto> Conditions => conditions;
    public IReadOnlyList<ItemEffectDto> Effects => effects;

    readonly List<UpgradeConditionDto> conditions = new();
    readonly List<ItemEffectDto> effects = new();

    public UpgradeInstance(UpgradeDto dto)
    {
        if (dto == null)
        {
            Id = string.Empty;
            Price = 0;
            return;
        }

        Id = dto.id;
        Price = dto.price;

        if (dto.conditions != null)
            conditions.AddRange(dto.conditions);

        if (dto.effects != null)
            effects.AddRange(dto.effects);
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
