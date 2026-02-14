using System;
using System.Collections.Generic;

public sealed class EffectApplier : IRuleEffectApplier
{
    public void ApplyEffect(EffectDef effect, RuleEffectApplyContext context)
    {
        if (effect == null || context == null || context.runState == null)
            return;

        RunState runState = context.runState;
        switch (effect.effectId)
        {
            case EffectIds.AddGold:
                runState.gold += ReadIntParam(effect.@params, 0);
                return;
            case EffectIds.AddStability:
                runState.stability += ReadIntParam(effect.@params, 0);
                return;

            case EffectIds.AddHpSelf:
                ApplyToSelf(context, target => target.hp += ReadIntParam(effect.@params, 0));
                return;
            case EffectIds.AddHpAssignedParty:
                ApplyToAssignedParty(runState, context.missionUid, target => target.hp += ReadIntParam(effect.@params, 0));
                return;
            case EffectIds.AddHpAllAdventurers:
                ApplyToAllAdventurers(runState, target => target.hp += ReadIntParam(effect.@params, 0));
                return;

            case EffectIds.AddStaminaSelf:
                ApplyToSelf(context, target => target.stamina += ReadIntParam(effect.@params, 0));
                return;
            case EffectIds.AddStaminaAssignedParty:
                ApplyToAssignedParty(runState, context.missionUid, target => target.stamina += ReadIntParam(effect.@params, 0));
                return;
            case EffectIds.AddStaminaAllAdventurers:
                ApplyToAllAdventurers(runState, target => target.stamina += ReadIntParam(effect.@params, 0));
                return;

            case EffectIds.AddXpSelf:
                ApplyToSelf(context, target => target.xp += ReadIntParam(effect.@params, 0));
                return;
            case EffectIds.AddXpAssignedParty:
                ApplyToAssignedParty(runState, context.missionUid, target => target.xp += ReadIntParam(effect.@params, 0));
                return;

            case EffectIds.AddAbilitySelf:
                ApplyAbilityToSelf(effect, context);
                return;
            case EffectIds.AddAbilityAssignedParty:
                ApplyAbilityToAssignedParty(effect, context);
                return;
            case EffectIds.AddAbilityAllAdventurers:
                ApplyAbilityToAllAdventurers(effect, context);
                return;
        }
    }

    static void ApplyAbilityToSelf(EffectDef effect, RuleEffectApplyContext context)
    {
        AdventurerInstance adventurer = FindAdventurer(context.runState, context.ownerAdventurerUid);
        if (adventurer == null)
            return;

        AddAbilityModifiers(effect, context, adventurer.uid);
    }

    static void ApplyAbilityToAssignedParty(EffectDef effect, RuleEffectApplyContext context)
    {
        MissionInstance mission = FindMission(context.runState, context.missionUid);
        if (mission == null || mission.assignedAdventurerUids == null || mission.assignedAdventurerUids.Count == 0)
            return;

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            string adventurerUid = mission.assignedAdventurerUids[i];
            if (string.IsNullOrWhiteSpace(adventurerUid))
                continue;

            AddAbilityModifiers(effect, context, adventurerUid);
        }
    }

    static void ApplyAbilityToAllAdventurers(EffectDef effect, RuleEffectApplyContext context)
    {
        if (context.runState.adventurers == null || context.runState.adventurers.Count == 0)
            return;

        for (int i = 0; i < context.runState.adventurers.Count; i++)
        {
            AdventurerInstance adventurer = context.runState.adventurers[i];
            if (adventurer == null || string.IsNullOrWhiteSpace(adventurer.uid))
                continue;

            AddAbilityModifiers(effect, context, adventurer.uid);
        }
    }

    static void AddAbilityModifiers(EffectDef effect, RuleEffectApplyContext context, string ownerUid)
    {
        if (context.runState.modifiers == null)
            context.runState.modifiers = new List<ModifierInstance>();

        AddOrMergeModifier(context.runState.modifiers, CreateAbilityModifier(effect, context, ownerUid, StatId.Strength, ReadIntParam(effect.@params, 0)));
        AddOrMergeModifier(context.runState.modifiers, CreateAbilityModifier(effect, context, ownerUid, StatId.Agility, ReadIntParam(effect.@params, 1)));
        AddOrMergeModifier(context.runState.modifiers, CreateAbilityModifier(effect, context, ownerUid, StatId.Intelligence, ReadIntParam(effect.@params, 2)));
    }

    static ModifierInstance CreateAbilityModifier(EffectDef effect, RuleEffectApplyContext context, string ownerUid, StatId statId, int amount)
    {
        if (amount == 0)
            return null;

        return new ModifierInstance
        {
            uid = Guid.NewGuid().ToString("N"),
            ownerUid = ownerUid,
            sourceUid = context.sourceUid ?? string.Empty,
            missionUid = context.missionUid ?? string.Empty,
            statId = statId,
            opType = ModifierOpType.Add,
            value = amount,
            priority = effect.priority,
            layer = ParseLayer(effect.layer),
            stackPolicy = ParseStackPolicy(effect.stackPolicy)
        };
    }

    static void AddOrMergeModifier(List<ModifierInstance> modifiers, ModifierInstance incoming)
    {
        if (incoming == null)
            return;

        int existingIndex = FindExistingModifierIndex(modifiers, incoming);
        switch (incoming.stackPolicy)
        {
            case ModifierStackPolicy.Stack:
                modifiers.Add(incoming);
                return;
            case ModifierStackPolicy.Replace:
                if (existingIndex >= 0)
                {
                    ModifierInstance existing = modifiers[existingIndex];
                    existing.value = incoming.value;
                    existing.priority = incoming.priority;
                    existing.layer = incoming.layer;
                    existing.opType = incoming.opType;
                    existing.missionUid = incoming.missionUid;
                }
                else
                {
                    modifiers.Add(incoming);
                }

                return;
            case ModifierStackPolicy.IgnoreIfExists:
                if (existingIndex < 0)
                    modifiers.Add(incoming);

                return;
            default:
                modifiers.Add(incoming);
                return;
        }
    }

    static int FindExistingModifierIndex(IReadOnlyList<ModifierInstance> modifiers, ModifierInstance probe)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            ModifierInstance item = modifiers[i];
            if (item == null)
                continue;

            if (!string.Equals(item.ownerUid, probe.ownerUid, StringComparison.Ordinal))
                continue;
            if (!string.Equals(item.sourceUid, probe.sourceUid, StringComparison.Ordinal))
                continue;
            if (!string.Equals(item.missionUid, probe.missionUid, StringComparison.Ordinal))
                continue;
            if (item.statId != probe.statId)
                continue;
            return i;
        }

        return -1;
    }

    static void ApplyToSelf(RuleEffectApplyContext context, Action<AdventurerInstance> apply)
    {
        if (apply == null)
            return;

        AdventurerInstance adventurer = FindAdventurer(context.runState, context.ownerAdventurerUid);
        if (adventurer == null)
            return;

        apply(adventurer);
    }

    static void ApplyToAssignedParty(RunState runState, string missionUid, Action<AdventurerInstance> apply)
    {
        if (apply == null)
            return;

        MissionInstance mission = FindMission(runState, missionUid);
        if (mission == null || mission.assignedAdventurerUids == null || mission.assignedAdventurerUids.Count == 0)
            return;

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            AdventurerInstance adventurer = FindAdventurer(runState, mission.assignedAdventurerUids[i]);
            if (adventurer == null)
                continue;

            apply(adventurer);
        }
    }

    static void ApplyToAllAdventurers(RunState runState, Action<AdventurerInstance> apply)
    {
        if (apply == null || runState.adventurers == null || runState.adventurers.Count == 0)
            return;

        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            AdventurerInstance adventurer = runState.adventurers[i];
            if (adventurer == null)
                continue;

            apply(adventurer);
        }
    }

    static AdventurerInstance FindAdventurer(RunState runState, string uid)
    {
        if (runState == null || runState.adventurers == null || string.IsNullOrWhiteSpace(uid))
            return null;

        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            AdventurerInstance entry = runState.adventurers[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, uid, StringComparison.Ordinal))
                return entry;
        }

        return null;
    }

    static MissionInstance FindMission(RunState runState, string uid)
    {
        if (runState == null || runState.missions == null || string.IsNullOrWhiteSpace(uid))
            return null;

        for (int i = 0; i < runState.missions.Count; i++)
        {
            MissionInstance entry = runState.missions[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, uid, StringComparison.Ordinal))
                return entry;
        }

        return null;
    }

    static ModifierLayer ParseLayer(string layer)
    {
        return string.Equals(layer, "mission", StringComparison.Ordinal)
            ? ModifierLayer.Mission
            : ModifierLayer.Normal;
    }

    static ModifierStackPolicy ParseStackPolicy(string stackPolicy)
    {
        if (string.Equals(stackPolicy, "replace", StringComparison.Ordinal))
            return ModifierStackPolicy.Replace;
        if (string.Equals(stackPolicy, "ignoreIfExists", StringComparison.Ordinal))
            return ModifierStackPolicy.IgnoreIfExists;
        return ModifierStackPolicy.Stack;
    }

    static int ReadIntParam(IReadOnlyList<float> values, int index)
    {
        if (values == null || index < 0 || index >= values.Count)
            return 0;

        return EffectMath.FloorToInt(values[index]);
    }
}
