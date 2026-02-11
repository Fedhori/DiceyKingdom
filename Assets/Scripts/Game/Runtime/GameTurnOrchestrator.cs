using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class GameTurnOrchestrator : MonoBehaviour
{
    [SerializeField] bool autoStartOnAwake = true;
    [SerializeField] bool useFixedSeed;
    [SerializeField] int fixedSeed = 1001;

    GameStaticDataSet staticData;
    Dictionary<string, SituationDef> situationDefById;
    Dictionary<string, AdventurerDef> adventurerDefById;
    Dictionary<string, SkillDef> skillDefById;
    System.Random rng;
    bool isRunOver;

    public GameRunState RunState { get; private set; }

    public event Action<GameRunState> RunStarted;
    public event Action<TurnPhase> PhaseChanged;
    public event Action<GameRunState> RunEnded;
    public event Action<int, string> StageSpawned;
    public event Action<int> AssignmentCommitConfirmationRequested;

    void Awake()
    {
        if (!autoStartOnAwake)
            return;

        StartNewRun();
    }

    public void StartNewRun(int? seedOverride = null)
    {
        int seed = seedOverride ?? (useFixedSeed ? fixedSeed : Environment.TickCount);

        staticData = GameStaticDataLoader.LoadAll();
        BuildDefinitionLookups(staticData);
        rng = new System.Random(seed);
        RunState = GameRunBootstrap.CreateNewRun(staticData, seed);
        isRunOver = false;

        RunStarted?.Invoke(RunState);
        StageSpawned?.Invoke(RunState.stage.stageNumber, RunState.stage.activePresetId);

        // TurnStart -> BoardUpdate -> AdventurerRoll까지 자동 진행.
        AdvanceToDecisionPoint();
    }

    public bool CommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;
        if (GetPendingAdventurerCount() > 0)
            return false;

        RunState.turn.processingAdventurerInstanceId = string.Empty;
        SetPhase(TurnPhase.Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public bool TryRollAdventurerBySlotIndex(int slotIndex)
    {
        if (RunState?.adventurers == null)
            return false;
        if (slotIndex < 0 || slotIndex >= RunState.adventurers.Count)
            return false;

        var adventurer = RunState.adventurers[slotIndex];
        if (adventurer == null)
            return false;

        return TryRollAdventurer(adventurer.instanceId);
    }

    public bool TryRollAdventurer(string adventurerInstanceId)
    {
        if (!CanAcceptRollInput())
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;
        if (!IsAdventurerPending(adventurerInstanceId))
            return false;

        var adventurer = FindAdventurerState(adventurerInstanceId);
        if (adventurer == null)
            return false;

        RunState.turn.processingAdventurerInstanceId = adventurer.instanceId;
        ExecuteRollPhase();

        if (adventurer.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
        {
            RunState.turn.processingAdventurerInstanceId = string.Empty;
            return false;
        }

        SetPhase(TurnPhase.Adjustment);
        return true;
    }

    public bool TryBeginAdventurerTargeting(string adventurerInstanceId)
    {
        if (!CanEnterTargetingInput())
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;
        if (!string.Equals(
                adventurerInstanceId,
                RunState.turn.processingAdventurerInstanceId,
                StringComparison.Ordinal))
        {
            return false;
        }

        SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    public bool TryAssignAdventurer(string adventurerInstanceId, string situationInstanceId)
    {
        if (!CanAcceptTargetingInput())
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return false;
        if (!string.Equals(
                adventurerInstanceId,
                RunState.turn.processingAdventurerInstanceId,
                StringComparison.Ordinal))
        {
            return false;
        }

        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null || adventurer.actionConsumed)
            return false;
        if (adventurer.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return false;

        var situation = FindSituationState(situationInstanceId);
        if (situation == null)
            return false;

        adventurer.assignedSituationInstanceId = situation.instanceId;
        int attackValue = SumDice(adventurer.rolledDiceValues);
        if (attackValue > 0)
        {
            ApplySituationRequirementDelta(
                situation.instanceId,
                -attackValue,
                markAssignedAsConsumedOnSuccess: false);
        }

        adventurer.assignedSituationInstanceId = null;
        adventurer.actionConsumed = true;
        RunState.turn.processingAdventurerInstanceId = string.Empty;

        if (GetPendingAdventurerCount() > 0)
        {
            SetPhase(TurnPhase.AdventurerRoll);
        }
        else
        {
            SetPhase(TurnPhase.Settlement);
            AdvanceToDecisionPoint();
        }

        return true;
    }

    public bool TryClearAdventurerAssignment(string adventurerInstanceId)
    {
        if (!CanAcceptTargetingInput())
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;
        if (!string.Equals(
                adventurerInstanceId,
                RunState.turn.processingAdventurerInstanceId,
                StringComparison.Ordinal))
        {
            return false;
        }

        SetPhase(TurnPhase.Adjustment);
        return true;
    }

    public int GetUnassignedAdventurerCount()
    {
        if (RunState == null)
            return 0;

        return GameAssignmentService.CountPendingAdventurers(RunState);
    }

    public bool RequestCommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;

        int pendingCount = GetUnassignedAdventurerCount();
        if (pendingCount > 0)
        {
            AssignmentCommitConfirmationRequested?.Invoke(pendingCount);
            return false;
        }

        return CommitAssignmentPhase();
    }

    public bool ConfirmCommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;

        MarkAllPendingAdventurersConsumed();
        SetPhase(TurnPhase.Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public bool CommitRollPhase()
    {
        return false;
    }

    public bool CommitAdjustmentPhase()
    {
        if (RunState == null || isRunOver)
            return false;
        if (RunState.turn.phase != TurnPhase.Adjustment)
            return false;
        if (FindCurrentProcessingAdventurer() == null)
            return false;

        SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    public bool TryUseSkill(
        string skillDefId,
        string selectedAdventurerInstanceId = null,
        string selectedSituationInstanceId = null,
        int selectedDieIndex = -1)
    {
        if (!CanAcceptSkillInput())
            return false;
        if (string.IsNullOrWhiteSpace(skillDefId))
            return false;

        if (!skillDefById.TryGetValue(skillDefId, out var skillDef))
            return false;

        var cooldownState = FindSkillCooldownState(skillDefId);
        if (cooldownState == null)
            return false;
        if (cooldownState.cooldownRemainingTurns > 0)
            return false;
        if (cooldownState.usedThisTurn)
            return false;
        if (!CanUseSkillInPhase(skillDef, RunState.turn.phase))
            return false;

        if (RunState.turn.phase == TurnPhase.Adjustment &&
            string.IsNullOrWhiteSpace(selectedAdventurerInstanceId))
        {
            selectedAdventurerInstanceId = RunState.turn.processingAdventurerInstanceId;
        }

        var context = new EffectTargetContext(
            selectedSituationInstanceId,
            selectedAdventurerInstanceId,
            selectedDieIndex);

        if (!TryApplyEffectBundle(skillDef.effectBundle, context))
            return false;

        cooldownState.cooldownRemainingTurns = Math.Max(0, skillDef.cooldownTurns);
        cooldownState.usedThisTurn = true;
        return true;
    }

    public bool TryUseSkillBySlotIndex(
        int skillSlotIndex,
        string selectedAdventurerInstanceId = null,
        string selectedSituationInstanceId = null,
        int selectedDieIndex = -1)
    {
        if (RunState?.skillCooldowns == null)
            return false;
        if (skillSlotIndex < 0 || skillSlotIndex >= RunState.skillCooldowns.Count)
            return false;

        var cooldown = RunState.skillCooldowns[skillSlotIndex];
        if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
            return false;

        return TryUseSkill(
            cooldown.skillDefId,
            selectedAdventurerInstanceId,
            selectedSituationInstanceId,
            selectedDieIndex);
    }

    public bool CanUseSkillBySlotIndex(int skillSlotIndex)
    {
        if (!CanAcceptSkillInput())
            return false;
        if (RunState?.skillCooldowns == null)
            return false;
        if (skillSlotIndex < 0 || skillSlotIndex >= RunState.skillCooldowns.Count)
            return false;

        var cooldown = RunState.skillCooldowns[skillSlotIndex];
        if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
            return false;
        if (cooldown.cooldownRemainingTurns > 0)
            return false;
        if (cooldown.usedThisTurn)
            return false;
        if (!skillDefById.TryGetValue(cooldown.skillDefId, out var skillDef))
            return false;

        return CanUseSkillInPhase(skillDef, RunState.turn.phase);
    }

    public bool SkillRequiresSituationTargetBySlotIndex(int skillSlotIndex)
    {
        if (RunState?.skillCooldowns == null)
            return false;
        if (skillSlotIndex < 0 || skillSlotIndex >= RunState.skillCooldowns.Count)
            return false;

        var cooldown = RunState.skillCooldowns[skillSlotIndex];
        if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
            return false;
        if (!skillDefById.TryGetValue(cooldown.skillDefId, out var skillDef))
            return false;

        return SkillRequiresSituationTarget(skillDef);
    }

    public void AdvanceToDecisionPoint()
    {
        if (RunState == null || isRunOver)
            return;

        bool keepRunning = true;
        while (keepRunning && !isRunOver)
        {
            switch (RunState.turn.phase)
            {
                case TurnPhase.TurnStart:
                    ExecuteTurnStartPhase();
                    break;

                case TurnPhase.BoardUpdate:
                    ExecuteBoardUpdatePhase();
                    break;

                case TurnPhase.AdventurerRoll:
                case TurnPhase.Adjustment:
                case TurnPhase.TargetAndAttack:
                    keepRunning = false;
                    break;

                case TurnPhase.Settlement:
                    ExecuteSettlementPhase();
                    break;

                case TurnPhase.EndTurn:
                    ExecuteEndTurnPhase();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    void ExecuteTurnStartPhase()
    {
        // 턴 시작 초기화: 배치/소비 상태 리셋 + 스킬 쿨다운 감소.
        RunState.ResetTurnTransientState();

        for (int i = 0; i < RunState.skillCooldowns.Count; i++)
        {
            var skillCooldown = RunState.skillCooldowns[i];
            if (skillCooldown == null)
                continue;

            if (skillCooldown.cooldownRemainingTurns > 0)
                skillCooldown.cooldownRemainingTurns -= 1;
        }

        SetPhase(TurnPhase.BoardUpdate);
    }

    void ExecuteBoardUpdatePhase()
    {
        RunState.turn.processingAdventurerInstanceId = string.Empty;

        bool spawned = GameRunBootstrap.TrySpawnPeriodicSituations(RunState, staticData, rng);
        if (spawned)
            StageSpawned?.Invoke(RunState.stage.stageNumber, RunState.stage.activePresetId);

        SetPhase(TurnPhase.AdventurerRoll);
    }

    void ExecuteRollPhase()
    {
        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null)
            return;

        adventurer.rolledDiceValues.Clear();
        adventurer.assignedSituationInstanceId = null;

        if (!adventurerDefById.TryGetValue(adventurer.adventurerDefId, out var adventurerDef))
            return;

        int diceCount = Math.Max(1, adventurerDef.diceCount);
        for (int dieIndex = 0; dieIndex < diceCount; dieIndex++)
            adventurer.rolledDiceValues.Add(RollD6());
    }

    void ExecuteSettlementPhase()
    {
        var situationOrder = new List<string>(RunState.situations.Count);
        for (int i = 0; i < RunState.situations.Count; i++)
        {
            var situation = RunState.situations[i];
            if (situation == null)
                continue;

            situationOrder.Add(situation.instanceId);
        }

        for (int i = 0; i < situationOrder.Count; i++)
        {
            string situationInstanceId = situationOrder[i];
            var situation = FindSituationState(situationInstanceId);
            if (situation == null)
                continue;
            if (situation.currentRequirement <= 0)
                continue;

            situation.deadlineTurnsLeft -= 1;
            if (situation.deadlineTurnsLeft > 0)
                continue;

            if (!situationDefById.TryGetValue(situation.situationDefId, out var situationDef))
            {
                RemoveSituationState(situation.instanceId);
                ClearAdventurerAssignmentsForSituation(situation.instanceId, markAssignedAsConsumed: false);
                continue;
            }

            var context = new EffectTargetContext(
                selectedSituationInstanceId: situation.instanceId,
                selectedAdventurerInstanceId: null,
                selectedDieIndex: -1);
            TryApplyEffectBundle(situationDef.failureEffect, context);

            situation = FindSituationState(situationInstanceId);
            if (situation == null)
                continue;

            HandleSituationFailurePostEffect(situation, situationDef);
        }

        SetPhase(TurnPhase.EndTurn);
    }

    void ExecuteEndTurnPhase()
    {
        NormalizeRunResources();

        if (IsRunGameOver())
        {
            isRunOver = true;
            RunEnded?.Invoke(RunState);
            return;
        }

        RunState.turn.turnNumber += 1;
        SetPhase(TurnPhase.TurnStart);
    }

    void SetPhase(TurnPhase phase)
    {
        RunState.turn.phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    bool CanAcceptRollInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (!string.IsNullOrWhiteSpace(RunState.turn.processingAdventurerInstanceId))
            return false;

        return RunState.turn.phase == TurnPhase.AdventurerRoll;
    }

    bool CanEnterTargetingInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (RunState.turn.phase != TurnPhase.Adjustment)
            return false;

        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null || adventurer.actionConsumed)
            return false;
        if (adventurer.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return false;

        return true;
    }

    bool CanAcceptTargetingInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (RunState.turn.phase != TurnPhase.TargetAndAttack)
            return false;

        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null || adventurer.actionConsumed)
            return false;
        if (adventurer.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return false;

        return true;
    }

    bool CanRequestTurnCommit()
    {
        if (RunState == null || isRunOver)
            return false;

        var phase = RunState.turn.phase;
        return phase == TurnPhase.AdventurerRoll ||
               phase == TurnPhase.Adjustment ||
               phase == TurnPhase.TargetAndAttack;
    }

    bool CanAcceptSkillInput()
    {
        if (RunState == null || isRunOver)
            return false;

        var phase = RunState.turn.phase;
        return phase == TurnPhase.AdventurerRoll ||
               phase == TurnPhase.Adjustment ||
               phase == TurnPhase.TargetAndAttack;
    }

    public bool CanRollAdventurer(string adventurerInstanceId)
    {
        if (!CanAcceptRollInput())
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;

        return IsAdventurerPending(adventurerInstanceId);
    }

    public bool CanAssignAdventurer(string adventurerInstanceId)
    {
        if (RunState == null || isRunOver)
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;
        if (!string.Equals(
                adventurerInstanceId,
                RunState.turn.processingAdventurerInstanceId,
                StringComparison.Ordinal))
        {
            return false;
        }

        var phase = RunState.turn.phase;
        if (phase != TurnPhase.Adjustment && phase != TurnPhase.TargetAndAttack)
            return false;

        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null || adventurer.actionConsumed)
            return false;

        return adventurer.rolledDiceValues != null && adventurer.rolledDiceValues.Count > 0;
    }

    public bool IsCurrentProcessingAdventurer(string adventurerInstanceId)
    {
        if (RunState == null)
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;

        return string.Equals(
            RunState.turn.processingAdventurerInstanceId,
            adventurerInstanceId,
            StringComparison.Ordinal);
    }

    int GetPendingAdventurerCount()
    {
        if (RunState == null)
            return 0;

        return GameAssignmentService.CountPendingAdventurers(RunState);
    }

    void MarkAllPendingAdventurersConsumed()
    {
        for (int i = 0; i < RunState.adventurers.Count; i++)
        {
            var adventurer = RunState.adventurers[i];
            if (adventurer == null || adventurer.actionConsumed)
                continue;

            adventurer.assignedSituationInstanceId = null;
            adventurer.actionConsumed = true;
        }

        RunState.turn.processingAdventurerInstanceId = string.Empty;
    }

    bool IsAdventurerPending(string adventurerInstanceId)
    {
        var adventurer = FindAdventurerState(adventurerInstanceId);
        if (adventurer == null)
            return false;

        return !adventurer.actionConsumed;
    }

    AdventurerState FindCurrentProcessingAdventurer()
    {
        if (RunState == null)
            return null;
        if (string.IsNullOrWhiteSpace(RunState.turn.processingAdventurerInstanceId))
            return null;

        return FindAdventurerState(RunState.turn.processingAdventurerInstanceId);
    }

    bool ResolveCurrentProcessingAdventurerId(string requestedAdventurerInstanceId, out string resolvedAdventurerInstanceId)
    {
        resolvedAdventurerInstanceId = null;

        if (RunState == null)
            return false;
        if (RunState.turn.phase != TurnPhase.Adjustment)
            return false;

        var currentAdventurerId = RunState.turn.processingAdventurerInstanceId;
        if (string.IsNullOrWhiteSpace(currentAdventurerId))
            return false;

        if (!string.IsNullOrWhiteSpace(requestedAdventurerInstanceId) &&
            !string.Equals(requestedAdventurerInstanceId, currentAdventurerId, StringComparison.Ordinal))
        {
            return false;
        }

        resolvedAdventurerInstanceId = currentAdventurerId;
        return true;
    }

    bool CanUseSkillInPhase(SkillDef skillDef, TurnPhase phase)
    {
        if (skillDef?.effectBundle?.effects == null)
            return false;

        for (int i = 0; i < skillDef.effectBundle.effects.Count; i++)
        {
            var effect = skillDef.effectBundle.effects[i];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                return false;

            string effectType = effect.effectType.Trim();
            bool allowed = effectType switch
            {
                "stability_delta" => phase == TurnPhase.AdventurerRoll || phase == TurnPhase.Adjustment || phase == TurnPhase.TargetAndAttack,
                "gold_delta" => phase == TurnPhase.AdventurerRoll || phase == TurnPhase.Adjustment || phase == TurnPhase.TargetAndAttack,
                "situation_requirement_delta" => phase == TurnPhase.AdventurerRoll || phase == TurnPhase.TargetAndAttack,
                "die_face_delta" => phase == TurnPhase.Adjustment,
                "reroll_adventurer_dice" => phase == TurnPhase.Adjustment,
                _ => false
            };

            if (!allowed)
                return false;
        }

        return true;
    }

    static bool SkillRequiresSituationTarget(SkillDef skillDef)
    {
        if (skillDef?.effectBundle?.effects == null)
            return false;

        for (int i = 0; i < skillDef.effectBundle.effects.Count; i++)
        {
            var effect = skillDef.effectBundle.effects[i];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                continue;

            if (string.Equals(
                    effect.effectType.Trim(),
                    "situation_requirement_delta",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    bool TryApplyEffectBundle(EffectBundle effectBundle, EffectTargetContext context)
    {
        if (effectBundle?.effects == null)
            return true;

        for (int i = 0; i < effectBundle.effects.Count; i++)
        {
            var effect = effectBundle.effects[i];
            if (!TryApplyEffect(effect, context))
                return false;
        }

        return true;
    }

    bool TryApplyEffect(EffectSpec effect, EffectTargetContext context)
    {
        if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
            return false;

        string effectType = effect.effectType.Trim();
        int value = ToIntValue(effect.value);

        if (effectType == "stability_delta")
            return TryApplyStabilityDelta(value);
        if (effectType == "gold_delta")
            return TryApplyGoldDelta(value);
        if (effectType == "situation_requirement_delta")
            return TryApplySituationRequirementDeltaEffect(effect, context, value);
        if (effectType == "die_face_delta")
            return TryApplyDieFaceDeltaEffect(effect, context, value);
        if (effectType == "reroll_adventurer_dice")
            return TryApplyRerollAdventurerDiceEffect(effect, context);

        return false;
    }

    bool TryApplyStabilityDelta(int delta)
    {
        int next = RunState.stability + delta;
        RunState.stability = ClampStability(next);
        return true;
    }

    bool TryApplyGoldDelta(int delta)
    {
        int next = RunState.gold + delta;
        RunState.gold = ClampGold(next);
        return true;
    }

    bool TryApplySituationRequirementDeltaEffect(EffectSpec effect, EffectTargetContext context, int delta)
    {
        string targetMode = GetParamString(effect.effectParams, "target_mode");
        if (!string.Equals(targetMode, "selected_situation", StringComparison.Ordinal))
            return false;

        string targetSituationInstanceId = context.selectedSituationInstanceId;
        if (string.IsNullOrWhiteSpace(targetSituationInstanceId))
            return false;
        if (FindSituationState(targetSituationInstanceId) == null)
            return false;

        ApplySituationRequirementDelta(
            targetSituationInstanceId,
            delta,
            markAssignedAsConsumedOnSuccess: false);
        return true;
    }

    bool TryApplyDieFaceDeltaEffect(EffectSpec effect, EffectTargetContext context, int delta)
    {
        string targetMode = GetParamString(effect.effectParams, "target_adventurer_mode");
        if (targetMode != "selected_adventurer")
            return false;
        if (!ResolveCurrentProcessingAdventurerId(context.selectedAdventurerInstanceId, out var adventurerInstanceId))
            return false;

        var adventurer = FindAdventurerState(adventurerInstanceId);
        if (adventurer == null || adventurer.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return false;

        string pickRule = GetParamString(effect.effectParams, "die_pick_rule");
        if (pickRule == "selected")
        {
            if (!IsValidDieIndex(adventurer, context.selectedDieIndex))
                return false;

            ApplyDieDelta(adventurer, context.selectedDieIndex, delta);
            return true;
        }

        if (pickRule == "lowest")
        {
            int index = GetLowestDieIndex(adventurer.rolledDiceValues);
            if (index < 0)
                return false;

            ApplyDieDelta(adventurer, index, delta);
            return true;
        }

        if (pickRule == "highest")
        {
            int index = GetHighestDieIndex(adventurer.rolledDiceValues);
            if (index < 0)
                return false;

            ApplyDieDelta(adventurer, index, delta);
            return true;
        }

        if (pickRule == "all")
        {
            for (int i = 0; i < adventurer.rolledDiceValues.Count; i++)
                ApplyDieDelta(adventurer, i, delta);
            return true;
        }

        return false;
    }

    bool TryApplyRerollAdventurerDiceEffect(EffectSpec effect, EffectTargetContext context)
    {
        string targetMode = GetParamString(effect.effectParams, "target_adventurer_mode");
        if (targetMode != "selected_adventurer")
            return false;
        if (!ResolveCurrentProcessingAdventurerId(context.selectedAdventurerInstanceId, out var adventurerInstanceId))
            return false;

        var adventurer = FindAdventurerState(adventurerInstanceId);
        if (adventurer == null || adventurer.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return false;

        string rerollRule = GetParamString(effect.effectParams, "reroll_rule");
        if (rerollRule == "all")
        {
            for (int i = 0; i < adventurer.rolledDiceValues.Count; i++)
                adventurer.rolledDiceValues[i] = RollD6();
            return true;
        }

        if (rerollRule == "single")
        {
            if (!IsValidDieIndex(adventurer, context.selectedDieIndex))
                return false;

            adventurer.rolledDiceValues[context.selectedDieIndex] = RollD6();
            return true;
        }

        return false;
    }

    bool ApplySituationRequirementDelta(
        string situationInstanceId,
        int delta,
        bool markAssignedAsConsumedOnSuccess)
    {
        var situation = FindSituationState(situationInstanceId);
        if (situation == null)
            return false;

        situation.currentRequirement += delta;
        if (situation.currentRequirement > 0)
            return true;

        HandleSituationSucceeded(situation.instanceId, markAssignedAsConsumedOnSuccess);
        return false;
    }

    void HandleSituationSucceeded(
        string situationInstanceId,
        bool markAssignedAsConsumedOnSuccess)
    {
        var situation = FindSituationState(situationInstanceId);
        if (situation == null)
            return;

        situationDefById.TryGetValue(situation.situationDefId, out var situationDef);

        RemoveSituationState(situation.instanceId);
        ClearAdventurerAssignmentsForSituation(situation.instanceId, markAssignedAsConsumedOnSuccess);

        if (situationDef == null)
            return;

        var context = new EffectTargetContext(
            selectedSituationInstanceId: situation.instanceId,
            selectedAdventurerInstanceId: null,
            selectedDieIndex: -1);
        TryApplyEffectBundle(situationDef.successReward, context);
    }

    void HandleSituationFailurePostEffect(SituationState situation, SituationDef situationDef)
    {
        if (situation == null)
            return;

        string mode = NormalizeFailurePersistMode(situationDef?.failurePersistMode);
        if (string.Equals(mode, "reset_deadline", StringComparison.Ordinal))
        {
            int baseDeadline = situationDef != null
                ? Math.Max(1, situationDef.baseDeadlineTurns)
                : 1;
            situation.deadlineTurnsLeft = baseDeadline;
            return;
        }

        RemoveSituationState(situation.instanceId);
        ClearAdventurerAssignmentsForSituation(situation.instanceId, markAssignedAsConsumed: false);
    }

    static string NormalizeFailurePersistMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "remove";

        string mode = raw.Trim();
        if (string.Equals(mode, "remove", StringComparison.OrdinalIgnoreCase))
            return "remove";
        if (string.Equals(mode, "reset_deadline", StringComparison.OrdinalIgnoreCase))
            return "reset_deadline";
        return "remove";
    }

    void RemoveSituationState(string situationInstanceId)
    {
        for (int i = RunState.situations.Count - 1; i >= 0; i--)
        {
            var situation = RunState.situations[i];
            if (situation == null)
                continue;
            if (!string.Equals(situation.instanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            RunState.situations.RemoveAt(i);
            break;
        }
    }

    void ClearAdventurerAssignmentsForSituation(string situationInstanceId, bool markAssignedAsConsumed)
    {
        for (int i = 0; i < RunState.adventurers.Count; i++)
        {
            var adventurer = RunState.adventurers[i];
            if (adventurer == null)
                continue;
            if (!string.Equals(adventurer.assignedSituationInstanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            adventurer.assignedSituationInstanceId = null;
            if (markAssignedAsConsumed)
                adventurer.actionConsumed = true;
        }
    }

    SkillCooldownState FindSkillCooldownState(string skillDefId)
    {
        for (int i = 0; i < RunState.skillCooldowns.Count; i++)
        {
            var cooldown = RunState.skillCooldowns[i];
            if (cooldown == null)
                continue;
            if (!string.Equals(cooldown.skillDefId, skillDefId, StringComparison.Ordinal))
                continue;

            return cooldown;
        }

        return null;
    }

    SituationState FindSituationState(string situationInstanceId)
    {
        for (int i = 0; i < RunState.situations.Count; i++)
        {
            var situation = RunState.situations[i];
            if (situation == null)
                continue;
            if (!string.Equals(situation.instanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            return situation;
        }

        return null;
    }

    AdventurerState FindAdventurerState(string adventurerInstanceId)
    {
        for (int i = 0; i < RunState.adventurers.Count; i++)
        {
            var adventurer = RunState.adventurers[i];
            if (adventurer == null)
                continue;
            if (!string.Equals(adventurer.instanceId, adventurerInstanceId, StringComparison.Ordinal))
                continue;

            return adventurer;
        }

        return null;
    }

    int RollD6()
    {
        return rng.Next(1, 7);
    }

    static int SumDice(List<int> dice)
    {
        if (dice == null || dice.Count == 0)
            return 0;

        int sum = 0;
        for (int i = 0; i < dice.Count; i++)
            sum += dice[i];

        return sum;
    }

    static int ToIntValue(double? value)
    {
        if (!value.HasValue)
            return 0;

        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    static string GetParamString(JObject effectParams, string key)
    {
        if (effectParams == null || string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var token = effectParams[key];
        if (token == null || token.Type == JTokenType.Null)
            return string.Empty;

        return token.ToString().Trim();
    }

    static bool IsValidDieIndex(AdventurerState adventurer, int dieIndex)
    {
        if (adventurer?.rolledDiceValues == null)
            return false;

        return dieIndex >= 0 && dieIndex < adventurer.rolledDiceValues.Count;
    }

    static int GetLowestDieIndex(IReadOnlyList<int> dice)
    {
        if (dice == null || dice.Count == 0)
            return -1;

        int index = 0;
        int value = dice[0];
        for (int i = 1; i < dice.Count; i++)
        {
            if (dice[i] >= value)
                continue;

            value = dice[i];
            index = i;
        }

        return index;
    }

    static int GetHighestDieIndex(IReadOnlyList<int> dice)
    {
        if (dice == null || dice.Count == 0)
            return -1;

        int index = 0;
        int value = dice[0];
        for (int i = 1; i < dice.Count; i++)
        {
            if (dice[i] <= value)
                continue;

            value = dice[i];
            index = i;
        }

        return index;
    }

    static void ApplyDieDelta(AdventurerState adventurer, int dieIndex, int delta)
    {
        int next = adventurer.rolledDiceValues[dieIndex] + delta;
        if (next < 1)
            next = 1;

        adventurer.rolledDiceValues[dieIndex] = next;
    }

    void BuildDefinitionLookups(GameStaticDataSet dataSet)
    {
        situationDefById = new Dictionary<string, SituationDef>(StringComparer.Ordinal);
        adventurerDefById = new Dictionary<string, AdventurerDef>(StringComparer.Ordinal);
        skillDefById = new Dictionary<string, SkillDef>(StringComparer.Ordinal);

        if (dataSet?.situationDefs != null)
        {
            for (int i = 0; i < dataSet.situationDefs.Count; i++)
            {
                var def = dataSet.situationDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.situationId))
                    continue;

                situationDefById[def.situationId] = def;
            }
        }

        if (dataSet?.adventurerDefs != null)
        {
            for (int i = 0; i < dataSet.adventurerDefs.Count; i++)
            {
                var def = dataSet.adventurerDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.adventurerId))
                    continue;

                adventurerDefById[def.adventurerId] = def;
            }
        }

        if (dataSet?.skillDefs != null)
        {
            for (int i = 0; i < dataSet.skillDefs.Count; i++)
            {
                var def = dataSet.skillDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.skillId))
                    continue;

                skillDefById[def.skillId] = def;
            }
        }
    }

    void NormalizeRunResources()
    {
        RunState.stability = ClampStability(RunState.stability);
        RunState.gold = ClampGold(RunState.gold);
    }

    bool IsRunGameOver()
    {
        return RunState.stability <= 0;
    }

    int ClampStability(int value)
    {
        if (value < 0)
            return 0;

        if (value > RunState.maxStability)
            return RunState.maxStability;

        return value;
    }

    static int ClampGold(int value)
    {
        return value < 0 ? 0 : value;
    }

    readonly struct EffectTargetContext
    {
        public readonly string selectedSituationInstanceId;
        public readonly string selectedAdventurerInstanceId;
        public readonly int selectedDieIndex;

        public EffectTargetContext(
            string selectedSituationInstanceId,
            string selectedAdventurerInstanceId,
            int selectedDieIndex)
        {
            this.selectedSituationInstanceId = selectedSituationInstanceId;
            this.selectedAdventurerInstanceId = selectedAdventurerInstanceId;
            this.selectedDieIndex = selectedDieIndex;
        }
    }
}

