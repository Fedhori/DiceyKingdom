using System;
using System.Collections.Generic;

public sealed class StatService
{
    readonly HashSet<string> dirtyOwnerUids = new(StringComparer.Ordinal);
    readonly Dictionary<string, Dictionary<StatId, int>> cachedStatsByOwnerUid = new(StringComparer.Ordinal);

    public void MarkDirty(string ownerUid)
    {
        if (string.IsNullOrWhiteSpace(ownerUid))
            return;

        dirtyOwnerUids.Add(ownerUid);
    }

    public void ClearCache()
    {
        dirtyOwnerUids.Clear();
        cachedStatsByOwnerUid.Clear();
    }

    public int GetStat(RunState runState, string ownerUid, StatId statId)
    {
        if (runState == null || string.IsNullOrWhiteSpace(ownerUid) || statId == StatId.None)
            return 0;

        if (!cachedStatsByOwnerUid.TryGetValue(ownerUid, out Dictionary<StatId, int> cacheByStat) ||
            dirtyOwnerUids.Contains(ownerUid) ||
            !cacheByStat.ContainsKey(statId))
        {
            RecalculateOwner(runState, ownerUid);
            cachedStatsByOwnerUid.TryGetValue(ownerUid, out cacheByStat);
        }

        if (cacheByStat != null && cacheByStat.TryGetValue(statId, out int value))
            return value;

        return 0;
    }

    public void RecalculateOwner(RunState runState, string ownerUid)
    {
        if (!TryGetAdventurer(runState, ownerUid, out AdventurerInstance adventurer))
        {
            cachedStatsByOwnerUid.Remove(ownerUid);
            dirtyOwnerUids.Remove(ownerUid);
            return;
        }

        var cache = cachedStatsByOwnerUid.TryGetValue(ownerUid, out Dictionary<StatId, int> existing)
            ? existing
            : new Dictionary<StatId, int>();

        cache[StatId.Strength] = CalculateStat(runState.modifiers, ownerUid, StatId.Strength, adventurer.strength);
        cache[StatId.Agility] = CalculateStat(runState.modifiers, ownerUid, StatId.Agility, adventurer.agility);
        cache[StatId.Intelligence] = CalculateStat(runState.modifiers, ownerUid, StatId.Intelligence, adventurer.intelligence);
        cache[StatId.Hp] = CalculateStat(runState.modifiers, ownerUid, StatId.Hp, adventurer.hp);
        cache[StatId.MaxHp] = CalculateStat(runState.modifiers, ownerUid, StatId.MaxHp, adventurer.maxHp);
        cache[StatId.Stamina] = CalculateStat(runState.modifiers, ownerUid, StatId.Stamina, adventurer.stamina);
        cache[StatId.MaxStamina] = CalculateStat(runState.modifiers, ownerUid, StatId.MaxStamina, adventurer.maxStamina);
        cache[StatId.Heroism] = CalculateStat(runState.modifiers, ownerUid, StatId.Heroism, adventurer.heroism);

        cachedStatsByOwnerUid[ownerUid] = cache;
        dirtyOwnerUids.Remove(ownerUid);
    }

    static int CalculateStat(IReadOnlyList<ModifierInstance> modifiers, string ownerUid, StatId statId, float baseValue)
    {
        float addSum = 0f;
        float mulProduct = 1f;
        bool hasSet = false;
        float setValue = 0f;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierInstance modifier = modifiers[i];
                if (modifier == null)
                    continue;

                if (!string.Equals(modifier.ownerUid, ownerUid, StringComparison.Ordinal))
                    continue;
                if (modifier.statId != statId)
                    continue;

                switch (modifier.opType)
                {
                    case ModifierOpType.Add:
                        addSum += modifier.value;
                        break;
                    case ModifierOpType.Mul:
                        mulProduct *= modifier.value;
                        break;
                    case ModifierOpType.Set:
                        hasSet = true;
                        setValue = modifier.value;
                        break;
                }
            }
        }

        float value = (baseValue + addSum) * mulProduct;
        if (hasSet)
            value = setValue;

        return EffectMath.FloorToInt(value);
    }

    static bool TryGetAdventurer(RunState runState, string ownerUid, out AdventurerInstance adventurer)
    {
        adventurer = null;
        if (runState?.adventurers == null || string.IsNullOrWhiteSpace(ownerUid))
            return false;

        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            AdventurerInstance entry = runState.adventurers[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, ownerUid, StringComparison.Ordinal))
            {
                adventurer = entry;
                return true;
            }
        }

        return false;
    }
}
