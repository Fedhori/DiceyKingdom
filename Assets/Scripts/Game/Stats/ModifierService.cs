using System;
using System.Collections.Generic;

/// <summary>
/// Adds, merges, and removes stat modifiers scoped to run entities.
/// </summary>
public sealed class ModifierService
{
    readonly StatService statService;

    public ModifierService(StatService statService)
    {
        this.statService = statService;
    }

    public void AddOrMergeModifier(RunState runState, ModifierInstance incoming)
    {
        if (runState == null || incoming == null)
            return;

        runState.modifiers ??= new List<ModifierInstance>();
        int existingIndex = FindExistingModifierIndex(runState.modifiers, incoming);

        switch (incoming.stackPolicy)
        {
            case ModifierStackPolicy.Stack:
                runState.modifiers.Add(incoming);
                MarkDirty(incoming.ownerUid);
                return;
            case ModifierStackPolicy.Replace:
                if (existingIndex >= 0)
                {
                    ModifierInstance existing = runState.modifiers[existingIndex];
                    existing.value = incoming.value;
                    existing.priority = incoming.priority;
                    existing.layer = incoming.layer;
                    existing.opType = incoming.opType;
                    existing.missionUid = incoming.missionUid;
                    existing.sourceUid = incoming.sourceUid;
                }
                else
                {
                    runState.modifiers.Add(incoming);
                }

                MarkDirty(incoming.ownerUid);
                return;
            case ModifierStackPolicy.IgnoreIfExists:
                if (existingIndex < 0)
                {
                    runState.modifiers.Add(incoming);
                    MarkDirty(incoming.ownerUid);
                }

                return;
            default:
                runState.modifiers.Add(incoming);
                MarkDirty(incoming.ownerUid);
                return;
        }
    }

    public int RemoveMissionLayerModifiers(RunState runState, string missionUid)
    {
        if (runState?.modifiers == null || runState.modifiers.Count == 0 || string.IsNullOrWhiteSpace(missionUid))
            return 0;

        int removed = 0;
        for (int i = runState.modifiers.Count - 1; i >= 0; i--)
        {
            ModifierInstance modifier = runState.modifiers[i];
            if (modifier == null)
                continue;

            if (modifier.layer != ModifierLayer.Mission)
                continue;
            if (!string.Equals(modifier.missionUid, missionUid, StringComparison.Ordinal))
                continue;

            runState.modifiers.RemoveAt(i);
            removed++;
            MarkDirty(modifier.ownerUid);
        }

        return removed;
    }

    public int RemoveModifiersBySourceUid(RunState runState, string sourceUid)
    {
        if (runState?.modifiers == null || runState.modifiers.Count == 0 || string.IsNullOrWhiteSpace(sourceUid))
            return 0;

        int removed = 0;
        for (int i = runState.modifiers.Count - 1; i >= 0; i--)
        {
            ModifierInstance modifier = runState.modifiers[i];
            if (modifier == null)
                continue;

            if (!string.Equals(modifier.sourceUid, sourceUid, StringComparison.Ordinal))
                continue;

            runState.modifiers.RemoveAt(i);
            removed++;
            MarkDirty(modifier.ownerUid);
        }

        return removed;
    }

    public int RemoveModifiersByOwnerUid(RunState runState, string ownerUid)
    {
        if (runState?.modifiers == null || runState.modifiers.Count == 0 || string.IsNullOrWhiteSpace(ownerUid))
            return 0;

        int removed = 0;
        for (int i = runState.modifiers.Count - 1; i >= 0; i--)
        {
            ModifierInstance modifier = runState.modifiers[i];
            if (modifier == null)
                continue;

            if (!string.Equals(modifier.ownerUid, ownerUid, StringComparison.Ordinal))
                continue;

            runState.modifiers.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
            MarkDirty(ownerUid);

        return removed;
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
            if (item.opType != probe.opType)
                continue;
            if (item.layer != probe.layer)
                continue;
            return i;
        }

        return -1;
    }

    void MarkDirty(string ownerUid)
    {
        statService?.MarkDirty(ownerUid);
    }
}

