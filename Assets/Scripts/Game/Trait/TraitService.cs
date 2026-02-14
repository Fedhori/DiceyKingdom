using System;
using System.Collections.Generic;
using UnityEngine;

public enum TraitRollOutcome
{
    None = 0,
    Positive = 1,
    Negative = 2
}

[Serializable]
public sealed class TraitRollResultEntry
{
    public string adventurerUid = string.Empty;
    public TraitRollOutcome outcome = TraitRollOutcome.None;
    public string gainedTraitId = string.Empty;
    public string removedTraitId = string.Empty;
}

public sealed class TraitService
{
    readonly ModifierService modifierService;

    public TraitService(ModifierService modifierService)
    {
        this.modifierService = modifierService;
    }

    public void ApplyExpeditionResultToParty(
        RunState runState,
        IReadOnlyList<string> adventurerUids,
        bool expeditionSucceeded,
        GameConfigData config,
        List<TraitRollResultEntry> results)
    {
        if (runState == null || adventurerUids == null || config == null || StaticDataLoader.Current == null)
            return;

        for (int i = 0; i < adventurerUids.Count; i++)
        {
            string adventurerUid = adventurerUids[i];
            if (string.IsNullOrWhiteSpace(adventurerUid))
                continue;

            if (!TryGetAdventurer(runState, adventurerUid, out AdventurerInstance adventurer))
                continue;

            if (adventurer.hp <= 0)
                continue;

            TraitRollResultEntry entry = RollAndApplySingle(runState, adventurer, expeditionSucceeded, config);
            if (entry != null && results != null)
                results.Add(entry);
        }
    }

    public bool SetTraitLocked(RunState runState, string traitUid, bool isLocked)
    {
        if (!TryGetTrait(runState, traitUid, out TraitInstance trait))
            return false;

        trait.isLocked = isLocked;
        return true;
    }

    public int RemoveTraitsByOwner(RunState runState, string ownerUid)
    {
        if (runState?.traits == null || runState.traits.Count == 0 || string.IsNullOrWhiteSpace(ownerUid))
            return 0;

        int removed = 0;
        for (int i = runState.traits.Count - 1; i >= 0; i--)
        {
            TraitInstance trait = runState.traits[i];
            if (trait == null)
                continue;

            if (!string.Equals(trait.ownerAdventurerUid, ownerUid, StringComparison.Ordinal))
                continue;

            RemoveUidFromOwner(runState, ownerUid, trait.uid);
            modifierService.RemoveModifiersBySourceUid(runState, trait.uid);
            runState.traits.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    TraitRollResultEntry RollAndApplySingle(RunState runState, AdventurerInstance adventurer, bool expeditionSucceeded, GameConfigData config)
    {
        TraitRollOutcome outcome = RollOutcome(expeditionSucceeded, config);
        var result = new TraitRollResultEntry
        {
            adventurerUid = adventurer.uid,
            outcome = outcome
        };

        if (outcome == TraitRollOutcome.None)
            return result;

        string polarity = outcome == TraitRollOutcome.Positive ? "positive" : "negative";
        TraitDef selectedDef = PickWeightedTraitDef(runState, adventurer, polarity);
        if (selectedDef == null)
            return result;

        int slotCount = Mathf.Max(0, config.traitSlotCount);
        adventurer.traitUids ??= new List<string>();

        if (adventurer.traitUids.Count >= slotCount)
        {
            string removedTraitUid = PickRandomUnlockedTraitUid(runState, adventurer);
            if (TryGetTrait(runState, removedTraitUid, out TraitInstance removingTrait))
                result.removedTraitId = removingTrait.traitId;

            RemoveTraitByUid(runState, removedTraitUid);
        }

        var instance = new TraitInstance
        {
            uid = Guid.NewGuid().ToString("N"),
            traitId = selectedDef.id,
            ownerAdventurerUid = adventurer.uid,
            isLocked = false
        };
        runState.traits.Add(instance);
        adventurer.traitUids.Add(instance.uid);
        result.gainedTraitId = selectedDef.id;
        return result;
    }

    TraitRollOutcome RollOutcome(bool expeditionSucceeded, GameConfigData config)
    {
        float none = expeditionSucceeded ? config.traitNoChangeOnSuccess : config.traitNoChangeOnFailure;
        float positive = expeditionSucceeded ? config.traitPositiveOnSuccess : config.traitPositiveOnFailure;
        float negative = expeditionSucceeded ? config.traitNegativeOnSuccess : config.traitNegativeOnFailure;

        float noneWeight = Mathf.Max(0f, none);
        float positiveWeight = Mathf.Max(0f, positive);
        float negativeWeight = Mathf.Max(0f, negative);
        float totalWeight = noneWeight + positiveWeight + negativeWeight;
        if (totalWeight <= 0f)
            return TraitRollOutcome.None;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        if (roll < noneWeight)
            return TraitRollOutcome.None;
        if (roll < noneWeight + positiveWeight)
            return TraitRollOutcome.Positive;
        return TraitRollOutcome.Negative;
    }

    TraitDef PickWeightedTraitDef(RunState runState, AdventurerInstance adventurer, string polarity)
    {
        IReadOnlyList<TraitDef> defs = StaticDataLoader.Current?.TraitDefs;
        if (defs == null || defs.Count == 0)
            return null;

        var ownedTraitIds = BuildOwnedTraitIdSet(runState, adventurer);
        var candidates = new List<TraitDef>();
        int totalWeight = 0;
        for (int i = 0; i < defs.Count; i++)
        {
            TraitDef def = defs[i];
            if (def == null)
                continue;
            if (!string.Equals(def.polarity, polarity, StringComparison.Ordinal))
                continue;
            if (ownedTraitIds.Contains(def.id))
                continue;

            candidates.Add(def);
            totalWeight += Mathf.Max(0, def.acquireWeight);
        }

        if (candidates.Count == 0)
            return null;

        if (totalWeight <= 0)
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            TraitDef def = candidates[i];
            cumulative += Mathf.Max(0, def.acquireWeight);
            if (roll < cumulative)
                return def;
        }

        return candidates[candidates.Count - 1];
    }

    HashSet<string> BuildOwnedTraitIdSet(RunState runState, AdventurerInstance adventurer)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (adventurer?.traitUids == null || adventurer.traitUids.Count == 0)
            return set;

        for (int i = 0; i < adventurer.traitUids.Count; i++)
        {
            string uid = adventurer.traitUids[i];
            if (TryGetTrait(runState, uid, out TraitInstance trait))
                set.Add(trait.traitId);
        }

        return set;
    }

    string PickRandomUnlockedTraitUid(RunState runState, AdventurerInstance adventurer)
    {
        var candidates = new List<string>();
        for (int i = 0; i < adventurer.traitUids.Count; i++)
        {
            string traitUid = adventurer.traitUids[i];
            if (!TryGetTrait(runState, traitUid, out TraitInstance trait))
                continue;
            if (trait.isLocked)
                continue;

            candidates.Add(traitUid);
        }

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }

    void RemoveTraitByUid(RunState runState, string traitUid)
    {
        if (!TryGetTrait(runState, traitUid, out TraitInstance trait))
            return;

        RemoveUidFromOwner(runState, trait.ownerAdventurerUid, trait.uid);
        modifierService.RemoveModifiersBySourceUid(runState, trait.uid);

        for (int i = runState.traits.Count - 1; i >= 0; i--)
        {
            TraitInstance entry = runState.traits[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, trait.uid, StringComparison.Ordinal))
            {
                runState.traits.RemoveAt(i);
                return;
            }
        }
    }

    static void RemoveUidFromOwner(RunState runState, string ownerUid, string traitUid)
    {
        if (!TryGetAdventurer(runState, ownerUid, out AdventurerInstance adventurer))
            return;
        if (adventurer.traitUids == null)
            return;

        for (int i = adventurer.traitUids.Count - 1; i >= 0; i--)
        {
            if (string.Equals(adventurer.traitUids[i], traitUid, StringComparison.Ordinal))
                adventurer.traitUids.RemoveAt(i);
        }
    }

    static bool TryGetAdventurer(RunState runState, string adventurerUid, out AdventurerInstance adventurer)
    {
        adventurer = null;
        if (runState?.adventurers == null || string.IsNullOrWhiteSpace(adventurerUid))
            return false;

        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            AdventurerInstance entry = runState.adventurers[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, adventurerUid, StringComparison.Ordinal))
            {
                adventurer = entry;
                return true;
            }
        }

        return false;
    }

    static bool TryGetTrait(RunState runState, string traitUid, out TraitInstance trait)
    {
        trait = null;
        if (runState?.traits == null || string.IsNullOrWhiteSpace(traitUid))
            return false;

        for (int i = 0; i < runState.traits.Count; i++)
        {
            TraitInstance entry = runState.traits[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, traitUid, StringComparison.Ordinal))
            {
                trait = entry;
                return true;
            }
        }

        return false;
    }
}
