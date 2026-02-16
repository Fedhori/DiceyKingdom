using System;
using System.Collections.Generic;
using UnityEngine;




public sealed class TurnLoopService
{
    readonly StatService statService;
    readonly ModifierService modifierService;
    readonly MissionExpeditionService missionExpeditionService;
    readonly TraitService traitService;

    public TurnLoopService(
        StatService statService,
        ModifierService modifierService,
        MissionExpeditionService missionExpeditionService,
        TraitService traitService)
    {
        this.statService = statService;
        this.modifierService = modifierService;
        this.missionExpeditionService = missionExpeditionService;
        this.traitService = traitService;
    }

    public bool InitializeRunLoop(RunState runState, GameConfigData config)
    {
        if (runState == null || config == null || StaticDataLoader.Current == null)
            return false;

        if (runState.barracksCapacity <= 0)
            runState.barracksCapacity = Mathf.Max(1, config.barracksCapacity);

        if (runState.turn <= 0)
            runState.turn = 1;

        SpawnCandidates(runState, Mathf.Max(0, config.candidateCountPerTurn));
        SpawnMissions(runState, Mathf.Max(0, config.missionSpawnCountPerTurn));
        return true;
    }

    public bool AdvanceTurn(RunState runState, GameConfigData config)
    {
        if (runState == null || config == null || StaticDataLoader.Current == null)
            return false;

        ApplyTurnSettlement(runState, config);
        missionExpeditionService.AdvanceMissionDeadlines(runState);
        RemoveUnrecruitedCandidates(runState);
        SpawnCandidates(runState, Mathf.Max(0, config.candidateCountPerTurn));
        SpawnMissions(runState, Mathf.Max(0, config.missionSpawnCountPerTurn));
        runState.turn = Mathf.Max(1, runState.turn + 1);
        return true;
    }

    public bool TryRecruitCandidate(RunState runState, string candidateUid)
    {
        if (runState == null || string.IsNullOrWhiteSpace(candidateUid))
            return false;

        int recruitedCount = GetRecruitedCount(runState);
        int capacity = Mathf.Max(1, runState.barracksCapacity);
        if (recruitedCount >= capacity)
            return false;

        runState.candidates ??= new List<AdventurerInstance>();
        if (!TryTakeAdventurer(runState.candidates, candidateUid, out AdventurerInstance candidate))
            return false;

        runState.adventurers ??= new List<AdventurerInstance>();
        runState.adventurers.Add(candidate);
        return true;
    }

    void ApplyTurnSettlement(RunState runState, GameConfigData config)
    {
        int globalHpRegen = Mathf.Max(0, config.globalHpRegenPerTurn);
        int restStaminaRegen = Mathf.Max(0, config.restStaminaRegenPerTurn);

        if (runState.adventurers == null)
            return;

        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            AdventurerInstance adventurer = runState.adventurers[i];
            if (adventurer == null)
                continue;

            if (adventurer.hp > 0)
            {
                adventurer.hp = Mathf.Clamp(adventurer.hp + globalHpRegen, 0, Mathf.Max(0, adventurer.maxHp));
                if (!adventurer.assignedThisTurn)
                    adventurer.stamina = Mathf.Clamp(adventurer.stamina + restStaminaRegen, 0, Mathf.Max(0, adventurer.maxStamina));
            }

            adventurer.assignedThisTurn = false;
            statService.MarkDirty(adventurer.uid);
        }
    }

    void RemoveUnrecruitedCandidates(RunState runState)
    {
        if (runState.candidates == null || runState.candidates.Count == 0)
            return;

        for (int i = runState.candidates.Count - 1; i >= 0; i--)
        {
            AdventurerInstance candidate = runState.candidates[i];
            if (candidate == null)
                continue;

            modifierService.RemoveModifiersByOwnerUid(runState, candidate.uid);
            traitService?.RemoveTraitsByOwner(runState, candidate.uid);
            runState.candidates.RemoveAt(i);
        }
    }

    void SpawnCandidates(RunState runState, int count)
    {
        runState.candidates ??= new List<AdventurerInstance>();

        for (int i = 0; i < count; i++)
        {
            AdventurerDef def = PickWeightedAdventurerDef();
            if (def == null)
                return;

            AdventurerInstance candidate = CreateAdventurerInstance(runState, def);
            runState.candidates.Add(candidate);
            statService.MarkDirty(candidate.uid);
        }
    }

    void SpawnMissions(RunState runState, int count)
    {
        for (int i = 0; i < count; i++)
        {
            MissionDef def = PickWeightedMissionDef();
            if (def == null)
                return;

            var mission = new MissionInstance
            {
                uid = Guid.NewGuid().ToString("N"),
                missionId = def.id,
                remainingDeadlineTurns = Mathf.Max(1, def.baseDeadlineTurns),
                isPartyLocked = false,
                isExpeditionInProgress = false,
                currentAbilityTestIndex = 0,
                assignedAdventurerUids = new List<string>(),
                abilityTestProgresses = new List<AbilityTestProgressInstance>()
            };

            if (def.abilityTests != null)
            {
                for (int testIndex = 0; testIndex < def.abilityTests.Count; testIndex++)
                {
                    mission.abilityTestProgresses.Add(new AbilityTestProgressInstance
                    {
                        testIndex = testIndex,
                        attemptCount = 0,
                        isCleared = false
                    });
                }
            }

            runState.missions.Add(mission);
        }
    }

    AdventurerInstance CreateAdventurerInstance(RunState runState, AdventurerDef def)
    {
        int maxHp = RandomRangeInclusive(def.baseHpMin, def.baseHpMax);
        int maxStamina = RandomRangeInclusive(def.baseStaminaMin, def.baseStaminaMax);
        var adventurer = new AdventurerInstance
        {
            uid = Guid.NewGuid().ToString("N"),
            adventurerId = def.id,
            level = 1,
            xp = 0,
            maxHp = maxHp,
            hp = maxHp,
            maxStamina = maxStamina,
            stamina = maxStamina,
            heroism = RandomRangeFloat(def.baseHeroismMin, def.baseHeroismMax),
            strength = RandomRangeInclusive(def.strengthMin, def.strengthMax),
            agility = RandomRangeInclusive(def.agilityMin, def.agilityMax),
            intelligence = RandomRangeInclusive(def.intelligenceMin, def.intelligenceMax),
            growthStrength = RandomRangeFloat(def.growthStrengthMin, def.growthStrengthMax),
            growthAgility = RandomRangeFloat(def.growthAgilityMin, def.growthAgilityMax),
            growthIntelligence = RandomRangeFloat(def.growthIntelligenceMin, def.growthIntelligenceMax),
            equipmentSlotCount = Mathf.Max(0, def.equipmentSlotCount),
            assignedThisTurn = false,
            assignedMissionUid = string.Empty,
            traitUids = new List<string>(),
            equipmentUids = new List<string>(),
            heroismUsedMissionUids = new List<string>()
        };

        AdventurerIdentityPool.AssignIdentity(runState, adventurer);
        return adventurer;
    }

    AdventurerDef PickWeightedAdventurerDef()
    {
        IReadOnlyList<AdventurerDef> defs = StaticDataLoader.Current?.AdventurerDefs;
        if (defs == null || defs.Count == 0)
            return null;

        int total = 0;
        for (int i = 0; i < defs.Count; i++)
            total += Mathf.Max(0, defs[i]?.recruitWeight ?? 0);

        if (total <= 0)
            return defs[UnityEngine.Random.Range(0, defs.Count)];

        int roll = UnityEngine.Random.Range(0, total);
        int cumulative = 0;
        for (int i = 0; i < defs.Count; i++)
        {
            AdventurerDef def = defs[i];
            if (def == null)
                continue;

            cumulative += Mathf.Max(0, def.recruitWeight);
            if (roll < cumulative)
                return def;
        }

        return defs[defs.Count - 1];
    }

    MissionDef PickWeightedMissionDef()
    {
        IReadOnlyList<MissionDef> defs = StaticDataLoader.Current?.MissionDefs;
        if (defs == null || defs.Count == 0)
            return null;

        int total = 0;
        for (int i = 0; i < defs.Count; i++)
            total += Mathf.Max(0, defs[i]?.spawnWeight ?? 0);

        if (total <= 0)
            return defs[UnityEngine.Random.Range(0, defs.Count)];

        int roll = UnityEngine.Random.Range(0, total);
        int cumulative = 0;
        for (int i = 0; i < defs.Count; i++)
        {
            MissionDef def = defs[i];
            if (def == null)
                continue;

            cumulative += Mathf.Max(0, def.spawnWeight);
            if (roll < cumulative)
                return def;
        }

        return defs[defs.Count - 1];
    }

    int GetRecruitedCount(RunState runState)
    {
        if (runState.adventurers == null || runState.adventurers.Count == 0)
            return 0;

        return runState.adventurers.Count;
    }

    static bool TryTakeAdventurer(List<AdventurerInstance> adventurers, string uid, out AdventurerInstance adventurer)
    {
        adventurer = null;
        if (adventurers == null || string.IsNullOrWhiteSpace(uid))
            return false;

        for (int i = adventurers.Count - 1; i >= 0; i--)
        {
            AdventurerInstance entry = adventurers[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, uid, StringComparison.Ordinal))
            {
                adventurer = entry;
                adventurers.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    static int RandomRangeInclusive(int min, int max)
    {
        if (min > max)
            (min, max) = (max, min);

        return UnityEngine.Random.Range(min, max + 1);
    }

    static float RandomRangeFloat(float min, float max)
    {
        if (min > max)
            (min, max) = (max, min);

        return UnityEngine.Random.Range(min, max);
    }
}

