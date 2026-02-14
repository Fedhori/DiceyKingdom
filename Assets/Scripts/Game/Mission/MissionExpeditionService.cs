using System;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityTestResolveOutcome
{
    Invalid = 0,
    TestSucceeded = 1,
    MissionSucceeded = 2,
    TestFailedBlockedByHeroism = 3,
    TestFailedWithDamage = 4,
    ExpeditionFailedAllDead = 5
}

public sealed class AbilityTestResolveResult
{
    public AbilityTestResolveOutcome outcome = AbilityTestResolveOutcome.Invalid;
    public string missionUid = string.Empty;
    public int testIndex = -1;
    public int partyAbilityValue;
    public int partyRoll;
    public int difficultyRoll;
    public bool heroismTriggered;
    public string heroismAdventurerUid = string.Empty;
    public int damagedCount;
    public int deadCount;
    public List<TraitRollResultEntry> traitResults = new();
}

public sealed class MissionExpeditionService
{
    readonly StatService statService;
    readonly ModifierService modifierService;
    readonly TraitService traitService;
    readonly Func<GameConfigData> getConfig;
    readonly Func<string, string, RuleContext, RuleExecutionSummary> runMissionTrigger;
    readonly Func<string, string, RuleContext, RuleExecutionSummary> runAdventurerTrigger;

    public MissionExpeditionService(
        StatService statService,
        ModifierService modifierService,
        TraitService traitService,
        Func<GameConfigData> getConfig,
        Func<string, string, RuleContext, RuleExecutionSummary> runMissionTrigger,
        Func<string, string, RuleContext, RuleExecutionSummary> runAdventurerTrigger)
    {
        this.statService = statService;
        this.modifierService = modifierService;
        this.traitService = traitService;
        this.getConfig = getConfig;
        this.runMissionTrigger = runMissionTrigger;
        this.runAdventurerTrigger = runAdventurerTrigger;
    }

    public bool TryAssignAdventurerToMission(RunState runState, string adventurerUid, string missionUid)
    {
        if (!TryGetMissionAndDef(runState, missionUid, out MissionInstance mission, out MissionDef missionDef))
            return false;

        if (mission.isPartyLocked)
            return false;

        if (!TryGetAdventurer(runState, adventurerUid, out AdventurerInstance adventurer))
            return false;

        if (adventurer.hp <= 0 || adventurer.assignedThisTurn)
            return false;

        if (!string.IsNullOrWhiteSpace(adventurer.assignedMissionUid) &&
            !string.Equals(adventurer.assignedMissionUid, missionUid, StringComparison.Ordinal))
        {
            if (!TryGetMission(runState, adventurer.assignedMissionUid, out MissionInstance previousMission))
                return false;

            if (previousMission.isPartyLocked)
                return false;

            RemoveUid(previousMission.assignedAdventurerUids, adventurer.uid);
        }

        mission.assignedAdventurerUids ??= new List<string>();
        if (ContainsUid(mission.assignedAdventurerUids, adventurer.uid))
        {
            adventurer.assignedMissionUid = mission.uid;
            return true;
        }

        if (mission.assignedAdventurerUids.Count >= missionDef.partyLimit)
            return false;

        mission.assignedAdventurerUids.Add(adventurer.uid);
        adventurer.assignedMissionUid = mission.uid;
        return true;
    }

    public bool TryUnassignAdventurer(RunState runState, string adventurerUid)
    {
        if (!TryGetAdventurer(runState, adventurerUid, out AdventurerInstance adventurer))
            return false;

        if (string.IsNullOrWhiteSpace(adventurer.assignedMissionUid))
            return false;

        if (!TryGetMission(runState, adventurer.assignedMissionUid, out MissionInstance mission))
        {
            adventurer.assignedMissionUid = string.Empty;
            return false;
        }

        if (mission.isPartyLocked)
            return false;

        RemoveUid(mission.assignedAdventurerUids, adventurer.uid);
        adventurer.assignedMissionUid = string.Empty;
        return true;
    }

    public AbilityTestResolveResult ResolveAbilityTestOnce(RunState runState, string missionUid)
    {
        var result = new AbilityTestResolveResult { missionUid = missionUid ?? string.Empty };
        if (!TryGetMissionAndDef(runState, missionUid, out MissionInstance mission, out MissionDef missionDef))
            return result;

        if (mission.assignedAdventurerUids == null || mission.assignedAdventurerUids.Count == 0)
            return result;

        EnsureAbilityTestProgresses(mission, missionDef);
        if (mission.currentAbilityTestIndex < 0)
            mission.currentAbilityTestIndex = 0;

        if (mission.currentAbilityTestIndex >= missionDef.abilityTests.Count)
            return result;

        if (!mission.isExpeditionInProgress)
        {
            mission.isExpeditionInProgress = true;
            mission.isPartyLocked = true;
            MarkPartyAsCommitted(runState, mission);
        }

        var calcContext = new RuleContext
        {
            runState = runState,
            missionUid = mission.uid
        };
        runMissionTrigger?.Invoke(mission.uid, RuleTriggerIds.OnAbilityValueCalculation, calcContext);

        int testIndex = mission.currentAbilityTestIndex;
        AbilityTestDef testDef = missionDef.abilityTests[testIndex];
        AbilityTestProgressInstance progress = mission.abilityTestProgresses[testIndex];
        progress.attemptCount++;

        int partyAbilityValue = CalculatePartyAbilityValue(runState, mission, testDef);
        int playerRollMax = Mathf.Max(1, partyAbilityValue);
        int difficultyRollMax = Mathf.Max(1, testDef.difficulty);
        int partyRoll = UnityEngine.Random.Range(1, playerRollMax + 1);
        int difficultyRoll = UnityEngine.Random.Range(1, difficultyRollMax + 1);

        result.testIndex = testIndex;
        result.partyAbilityValue = partyAbilityValue;
        result.partyRoll = partyRoll;
        result.difficultyRoll = difficultyRoll;

        if (partyRoll >= difficultyRoll)
        {
            progress.isCleared = true;
            mission.currentAbilityTestIndex++;

            if (mission.currentAbilityTestIndex >= missionDef.abilityTests.Count)
            {
                CompleteExpeditionSuccess(runState, mission, result.traitResults);
                result.outcome = AbilityTestResolveOutcome.MissionSucceeded;
            }
            else
            {
                result.outcome = AbilityTestResolveOutcome.TestSucceeded;
            }

            return result;
        }

        if (TryTriggerHeroism(runState, mission, out string heroismUid))
        {
            result.heroismTriggered = true;
            result.heroismAdventurerUid = heroismUid;
            result.outcome = AbilityTestResolveOutcome.TestFailedBlockedByHeroism;
            return result;
        }

        List<string> deadUids = ApplyFailureDamage(runState, mission, result);
        result.deadCount = deadUids.Count;
        for (int i = 0; i < deadUids.Count; i++)
            MoveAdventurerToGraveyard(runState, deadUids[i]);

        if (!HasAssignedLivingAdventurer(runState, mission))
        {
            FailExpeditionInternal(runState, mission.uid, result.traitResults);
            result.outcome = AbilityTestResolveOutcome.ExpeditionFailedAllDead;
            return result;
        }

        result.outcome = AbilityTestResolveOutcome.TestFailedWithDamage;
        return result;
    }

    public bool FailExpedition(RunState runState, string missionUid)
    {
        return FailExpeditionInternal(runState, missionUid, null);
    }

    public int AdvanceMissionDeadlines(RunState runState)
    {
        if (runState?.missions == null || runState.missions.Count == 0)
            return 0;

        var failedMissionUids = new List<string>();
        for (int i = 0; i < runState.missions.Count; i++)
        {
            MissionInstance mission = runState.missions[i];
            if (mission == null)
                continue;

            mission.remainingDeadlineTurns--;
            if (mission.remainingDeadlineTurns <= 0)
                failedMissionUids.Add(mission.uid);
        }

        int removed = 0;
        for (int i = 0; i < failedMissionUids.Count; i++)
        {
            string missionUid = failedMissionUids[i];
            if (!TryGetMission(runState, missionUid, out MissionInstance mission))
                continue;

            var context = new RuleContext
            {
                runState = runState,
                missionUid = mission.uid
            };
            runMissionTrigger?.Invoke(mission.uid, RuleTriggerIds.OnMissionFailed, context);

            modifierService.RemoveMissionLayerModifiers(runState, mission.uid);
            ClearMissionAssignments(runState, mission);
            RemoveMissionByUid(runState, mission.uid);
            removed++;
        }

        return removed;
    }

    void CompleteExpeditionSuccess(RunState runState, MissionInstance mission, List<TraitRollResultEntry> traitResults)
    {
        var context = new RuleContext
        {
            runState = runState,
            missionUid = mission.uid,
            expeditionSucceeded = true
        };
        runMissionTrigger?.Invoke(mission.uid, RuleTriggerIds.OnExpeditionResolved, context);
        ApplyTraitResult(runState, mission, true, traitResults);

        modifierService.RemoveMissionLayerModifiers(runState, mission.uid);
        ClearMissionAssignments(runState, mission);
        RemoveMissionByUid(runState, mission.uid);
    }

    bool FailExpeditionInternal(RunState runState, string missionUid, List<TraitRollResultEntry> traitResults)
    {
        if (!TryGetMission(runState, missionUid, out MissionInstance mission))
            return false;

        var context = new RuleContext
        {
            runState = runState,
            missionUid = mission.uid,
            expeditionSucceeded = false
        };
        runMissionTrigger?.Invoke(mission.uid, RuleTriggerIds.OnExpeditionResolved, context);
        ApplyTraitResult(runState, mission, false, traitResults);

        modifierService.RemoveMissionLayerModifiers(runState, mission.uid);
        ClearMissionAssignments(runState, mission);
        mission.isExpeditionInProgress = false;
        mission.isPartyLocked = false;
        return true;
    }

    void ApplyTraitResult(RunState runState, MissionInstance mission, bool expeditionSucceeded, List<TraitRollResultEntry> traitResults)
    {
        if (traitService == null || mission == null)
            return;

        GameConfigData config = getConfig?.Invoke();
        if (config == null)
            return;

        traitService.ApplyExpeditionResultToParty(
            runState,
            mission.assignedAdventurerUids,
            expeditionSucceeded,
            config,
            traitResults);
    }

    void MarkPartyAsCommitted(RunState runState, MissionInstance mission)
    {
        if (mission.assignedAdventurerUids == null)
            return;

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            if (!TryGetAdventurer(runState, mission.assignedAdventurerUids[i], out AdventurerInstance adventurer))
                continue;

            adventurer.assignedThisTurn = true;
        }
    }

    int CalculatePartyAbilityValue(RunState runState, MissionInstance mission, AbilityTestDef testDef)
    {
        if (mission.assignedAdventurerUids == null || testDef == null || testDef.requiredAbilities == null)
            return 0;

        int total = 0;
        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            string uid = mission.assignedAdventurerUids[i];
            if (!TryGetAdventurer(runState, uid, out _))
                continue;

            for (int abilityIndex = 0; abilityIndex < testDef.requiredAbilities.Count; abilityIndex++)
            {
                StatId statId = ToStatId(testDef.requiredAbilities[abilityIndex]);
                if (statId == StatId.None)
                    continue;

                total += Mathf.Max(0, statService.GetStat(runState, uid, statId));
            }
        }

        return total;
    }

    bool TryTriggerHeroism(RunState runState, MissionInstance mission, out string heroismUid)
    {
        heroismUid = string.Empty;
        if (mission.assignedAdventurerUids == null)
            return false;

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            string uid = mission.assignedAdventurerUids[i];
            if (!TryGetAdventurer(runState, uid, out AdventurerInstance adventurer))
                continue;
            if (adventurer.hp <= 0)
                continue;

            adventurer.heroismUsedMissionUids ??= new List<string>();
            if (ContainsUid(adventurer.heroismUsedMissionUids, mission.uid))
                continue;

            float chance = Mathf.Clamp01(adventurer.heroism);
            if (UnityEngine.Random.value < chance)
            {
                adventurer.heroismUsedMissionUids.Add(mission.uid);
                heroismUid = adventurer.uid;
                return true;
            }
        }

        return false;
    }

    List<string> ApplyFailureDamage(RunState runState, MissionInstance mission, AbilityTestResolveResult result)
    {
        var deadUids = new List<string>();
        if (mission.assignedAdventurerUids == null)
            return deadUids;

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            string uid = mission.assignedAdventurerUids[i];
            if (!TryGetAdventurer(runState, uid, out AdventurerInstance adventurer))
                continue;
            if (adventurer.hp <= 0)
                continue;

            adventurer.hp = Mathf.Max(0, adventurer.hp - 1);
            statService.MarkDirty(adventurer.uid);
            result.damagedCount++;

            var hpContext = new RuleContext
            {
                runState = runState,
                missionUid = mission.uid,
                adventurerUid = adventurer.uid,
                hpDelta = -1
            };
            runAdventurerTrigger?.Invoke(adventurer.uid, RuleTriggerIds.OnHpChanged, hpContext);

            if (adventurer.hp <= 0)
                deadUids.Add(adventurer.uid);
        }

        return deadUids;
    }

    bool HasAssignedLivingAdventurer(RunState runState, MissionInstance mission)
    {
        if (mission.assignedAdventurerUids == null || mission.assignedAdventurerUids.Count == 0)
            return false;

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            if (!TryGetAdventurer(runState, mission.assignedAdventurerUids[i], out AdventurerInstance adventurer))
                continue;

            if (adventurer.hp > 0)
                return true;
        }

        return false;
    }

    void MoveAdventurerToGraveyard(RunState runState, string adventurerUid)
    {
        if (string.IsNullOrWhiteSpace(adventurerUid))
            return;

        runState.graveyard ??= new List<AdventurerInstance>();
        AdventurerInstance deadAdventurer = null;

        for (int i = runState.adventurers.Count - 1; i >= 0; i--)
        {
            AdventurerInstance adventurer = runState.adventurers[i];
            if (adventurer == null)
                continue;

            if (!string.Equals(adventurer.uid, adventurerUid, StringComparison.Ordinal))
                continue;

            deadAdventurer = adventurer;
            runState.adventurers.RemoveAt(i);
            break;
        }

        if (deadAdventurer != null)
            runState.graveyard.Add(deadAdventurer);

        for (int i = 0; i < runState.missions.Count; i++)
            RemoveUid(runState.missions[i]?.assignedAdventurerUids, adventurerUid);

        modifierService.RemoveModifiersByOwnerUid(runState, adventurerUid);
        traitService?.RemoveTraitsByOwner(runState, adventurerUid);
    }

    void ClearMissionAssignments(RunState runState, MissionInstance mission)
    {
        if (mission.assignedAdventurerUids == null)
            mission.assignedAdventurerUids = new List<string>();

        for (int i = 0; i < mission.assignedAdventurerUids.Count; i++)
        {
            string uid = mission.assignedAdventurerUids[i];
            if (!TryGetAdventurer(runState, uid, out AdventurerInstance adventurer))
                continue;

            if (string.Equals(adventurer.assignedMissionUid, mission.uid, StringComparison.Ordinal))
                adventurer.assignedMissionUid = string.Empty;
        }

        mission.assignedAdventurerUids.Clear();
    }

    void EnsureAbilityTestProgresses(MissionInstance mission, MissionDef missionDef)
    {
        mission.abilityTestProgresses ??= new List<AbilityTestProgressInstance>();
        if (missionDef?.abilityTests == null)
            return;

        while (mission.abilityTestProgresses.Count < missionDef.abilityTests.Count)
        {
            mission.abilityTestProgresses.Add(new AbilityTestProgressInstance
            {
                testIndex = mission.abilityTestProgresses.Count
            });
        }
    }

    static StatId ToStatId(string abilityId)
    {
        if (string.Equals(abilityId, "strength", StringComparison.Ordinal))
            return StatId.Strength;
        if (string.Equals(abilityId, "agility", StringComparison.Ordinal))
            return StatId.Agility;
        if (string.Equals(abilityId, "intelligence", StringComparison.Ordinal))
            return StatId.Intelligence;
        return StatId.None;
    }

    static void RemoveMissionByUid(RunState runState, string missionUid)
    {
        if (runState?.missions == null || string.IsNullOrWhiteSpace(missionUid))
            return;

        for (int i = runState.missions.Count - 1; i >= 0; i--)
        {
            MissionInstance mission = runState.missions[i];
            if (mission == null)
                continue;

            if (string.Equals(mission.uid, missionUid, StringComparison.Ordinal))
            {
                runState.missions.RemoveAt(i);
                return;
            }
        }
    }

    static bool ContainsUid(IReadOnlyList<string> list, string uid)
    {
        if (list == null || string.IsNullOrWhiteSpace(uid))
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], uid, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static void RemoveUid(List<string> list, string uid)
    {
        if (list == null || string.IsNullOrWhiteSpace(uid))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (string.Equals(list[i], uid, StringComparison.Ordinal))
                list.RemoveAt(i);
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

    static bool TryGetMission(RunState runState, string missionUid, out MissionInstance mission)
    {
        mission = null;
        if (runState?.missions == null || string.IsNullOrWhiteSpace(missionUid))
            return false;

        for (int i = 0; i < runState.missions.Count; i++)
        {
            MissionInstance entry = runState.missions[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, missionUid, StringComparison.Ordinal))
            {
                mission = entry;
                return true;
            }
        }

        return false;
    }

    static bool TryGetMissionAndDef(RunState runState, string missionUid, out MissionInstance mission, out MissionDef missionDef)
    {
        mission = null;
        missionDef = null;
        if (!TryGetMission(runState, missionUid, out mission))
            return false;

        if (StaticDataLoader.Current == null)
            return false;

        return StaticDataLoader.Current.TryGetMissionDef(mission.missionId, out missionDef);
    }
}
