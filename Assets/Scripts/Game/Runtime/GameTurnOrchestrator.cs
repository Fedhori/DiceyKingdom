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
    Dictionary<string, EnemyDef> enemyDefById;
    Dictionary<string, AdventurerDef> adventurerDefById;
    Dictionary<string, SkillDef> skillDefById;
    System.Random rng;
    bool isRunOver;

    public GameRunState RunState { get; private set; }

    public event Action<GameRunState> RunStarted;
    public event Action<TurnPhase> PhaseChanged;
    public event Action<GameRunState> RunEnded;
    public event Action<int, string> StageSpawned;
    public event Action<string, string> AssignmentChanged;
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

        // P0 -> P1 -> P2까지 자동 진행.
        AdvanceToDecisionPoint();
    }

    public bool CommitAssignmentPhase()
    {
        if (!CanAcceptAssignmentInput())
            return false;
        if (GetPendingAdventurerCount() > 0)
            return false;

        SetPhase(TurnPhase.P5Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public bool TryAssignAdventurer(string adventurerInstanceId, string enemyInstanceId)
    {
        if (!CanAcceptAssignmentInput())
            return false;
        if (!IsAdventurerPending(adventurerInstanceId))
            return false;
        if (!string.IsNullOrWhiteSpace(RunState.turn.processingAdventurerInstanceId))
            return false;

        var result = GameAssignmentService.AssignAdventurerToEnemy(RunState, adventurerInstanceId, enemyInstanceId);
        if (result != AssignmentResult.Success)
            return false;

        if (!TryMoveToPhase(TurnPhase.P2Assignment, TurnPhase.P3Roll))
        {
            GameAssignmentService.ClearAdventurerAssignment(RunState, adventurerInstanceId);
            AssignmentChanged?.Invoke(adventurerInstanceId, null);
            return false;
        }

        RunState.turn.processingAdventurerInstanceId = adventurerInstanceId;
        AssignmentChanged?.Invoke(adventurerInstanceId, enemyInstanceId);
        ExecuteP3Roll();
        SetPhase(TurnPhase.P4Adjustment);
        return true;
    }

    public bool TryClearAdventurerAssignment(string adventurerInstanceId)
    {
        if (!CanAcceptAssignmentInput())
            return false;
        if (!IsAdventurerPending(adventurerInstanceId))
            return false;

        var result = GameAssignmentService.ClearAdventurerAssignment(RunState, adventurerInstanceId);
        if (result != AssignmentResult.Success)
            return false;

        AssignmentChanged?.Invoke(adventurerInstanceId, null);
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
        if (!CanAcceptAssignmentInput())
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
        if (!CanAcceptAssignmentInput())
            return false;

        MarkAllPendingAdventurersConsumed();
        SetPhase(TurnPhase.P5Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public bool CommitRollPhase()
    {
        if (RunState == null || isRunOver)
            return false;
        if (RunState.turn.phase != TurnPhase.P3Roll)
            return false;

        ExecuteP3Roll();
        SetPhase(TurnPhase.P4Adjustment);
        return true;
    }

    public bool CommitAdjustmentPhase()
    {
        if (RunState == null || isRunOver)
            return false;
        if (RunState.turn.phase != TurnPhase.P4Adjustment)
            return false;

        ExecuteP4Adjustment();
        if (GetPendingAdventurerCount() > 0)
        {
            SetPhase(TurnPhase.P2Assignment);
        }
        else
        {
            SetPhase(TurnPhase.P5Settlement);
            // P5, P6, 다음 턴 P0/P1까지 자동 처리한 뒤 P2에서 대기.
            AdvanceToDecisionPoint();
        }

        return true;
    }

    public bool TryUseSkill(
        string skillDefId,
        string selectedAdventurerInstanceId = null,
        string selectedEnemyInstanceId = null,
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

        if (RunState.turn.phase == TurnPhase.P4Adjustment &&
            string.IsNullOrWhiteSpace(selectedAdventurerInstanceId))
        {
            selectedAdventurerInstanceId = RunState.turn.processingAdventurerInstanceId;
        }

        var context = new EffectTargetContext(
            selectedEnemyInstanceId,
            selectedAdventurerInstanceId,
            selectedDieIndex,
            null);

        if (!TryApplyEffectBundle(skillDef.effectBundle, context))
            return false;

        cooldownState.cooldownRemainingTurns = Math.Max(0, skillDef.cooldownTurns);
        cooldownState.usedThisTurn = true;
        return true;
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
                case TurnPhase.P0TurnStart:
                    ExecuteP0TurnStart();
                    break;

                case TurnPhase.P1BoardUpdate:
                    ExecuteP1BoardUpdate();
                    break;

                case TurnPhase.P2Assignment:
                case TurnPhase.P3Roll:
                case TurnPhase.P4Adjustment:
                    keepRunning = false;
                    break;

                case TurnPhase.P5Settlement:
                    ExecuteP5Settlement();
                    break;

                case TurnPhase.P6EndTurn:
                    ExecuteP6EndTurn();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    bool TryMoveToPhase(TurnPhase current, TurnPhase next)
    {
        if (RunState == null || isRunOver)
            return false;
        if (RunState.turn.phase != current)
            return false;

        SetPhase(next);
        return true;
    }

    void ExecuteP0TurnStart()
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

        SetPhase(TurnPhase.P1BoardUpdate);
    }

    void ExecuteP1BoardUpdate()
    {
        RunState.turn.processingAdventurerInstanceId = string.Empty;

        bool spawned = GameRunBootstrap.TrySpawnStagePresetIfBoardCleared(RunState, staticData, rng);
        if (spawned)
            StageSpawned?.Invoke(RunState.stage.stageNumber, RunState.stage.activePresetId);

        SetPhase(TurnPhase.P2Assignment);
    }

    void ExecuteP3Roll()
    {
        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null)
            return;

        adventurer.rolledDiceValues.Clear();

        if (string.IsNullOrWhiteSpace(adventurer.assignedEnemyInstanceId))
            return;

        var enemy = FindEnemyState(adventurer.assignedEnemyInstanceId);
        if (enemy == null)
        {
            GameAssignmentService.ClearAdventurerAssignment(RunState, adventurer.instanceId);
            AssignmentChanged?.Invoke(adventurer.instanceId, null);
            return;
        }

        if (!adventurerDefById.TryGetValue(adventurer.adventurerDefId, out var adventurerDef))
            return;

        int diceCount = Math.Max(1, adventurerDef.diceCount);
        for (int dieIndex = 0; dieIndex < diceCount; dieIndex++)
            adventurer.rolledDiceValues.Add(RollD6());
    }

    void ExecuteP4Adjustment()
    {
        var adventurer = FindCurrentProcessingAdventurer();
        if (adventurer == null)
        {
            RunState.turn.processingAdventurerInstanceId = string.Empty;
            return;
        }

        string assignedEnemyInstanceId = adventurer.assignedEnemyInstanceId;
        int attackValue = SumDice(adventurer.rolledDiceValues);

        if (!string.IsNullOrWhiteSpace(assignedEnemyInstanceId) && attackValue > 0)
        {
            ApplyEnemyHealthDelta(
                assignedEnemyInstanceId,
                -attackValue,
                rewardOnKill: true,
                markAssignedAsConsumedOnKill: false);
        }

        if (!string.IsNullOrWhiteSpace(adventurer.assignedEnemyInstanceId))
        {
            GameAssignmentService.ClearAdventurerAssignment(RunState, adventurer.instanceId);
            AssignmentChanged?.Invoke(adventurer.instanceId, null);
        }

        adventurer.actionConsumed = true;
        RunState.turn.processingAdventurerInstanceId = string.Empty;
    }

    void ExecuteP5Settlement()
    {
        // 액션 발동/정산은 17, 18번 작업에서 구현.
        SetPhase(TurnPhase.P6EndTurn);
    }

    void ExecuteP6EndTurn()
    {
        if (RunState.stability <= 0)
        {
            isRunOver = true;
            RunEnded?.Invoke(RunState);
            return;
        }

        RunState.turn.turnNumber += 1;
        SetPhase(TurnPhase.P0TurnStart);
    }

    void SetPhase(TurnPhase phase)
    {
        RunState.turn.phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    bool CanAcceptAssignmentInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (!string.IsNullOrWhiteSpace(RunState.turn.processingAdventurerInstanceId))
            return false;

        return RunState.turn.phase == TurnPhase.P2Assignment;
    }

    bool CanAcceptSkillInput()
    {
        if (RunState == null || isRunOver)
            return false;

        var phase = RunState.turn.phase;
        return phase == TurnPhase.P2Assignment || phase == TurnPhase.P4Adjustment;
    }

    public bool CanAssignAdventurer(string adventurerInstanceId)
    {
        if (!CanAcceptAssignmentInput())
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;

        return IsAdventurerPending(adventurerInstanceId);
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

            GameAssignmentService.ClearAdventurerAssignment(RunState, adventurer.instanceId);
            AssignmentChanged?.Invoke(adventurer.instanceId, null);
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
        if (RunState.turn.phase != TurnPhase.P4Adjustment)
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
                "stability_delta" => phase == TurnPhase.P2Assignment || phase == TurnPhase.P4Adjustment,
                "gold_delta" => phase == TurnPhase.P2Assignment || phase == TurnPhase.P4Adjustment,
                "enemy_health_delta" => phase == TurnPhase.P2Assignment || phase == TurnPhase.P4Adjustment,
                "die_face_delta" => phase == TurnPhase.P4Adjustment,
                "reroll_adventurer_dice" => phase == TurnPhase.P4Adjustment,
                _ => false
            };

            if (!allowed)
                return false;
        }

        return true;
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
        if (effectType == "enemy_health_delta")
            return TryApplyEnemyHealthDeltaEffect(effect, context, value);
        if (effectType == "die_face_delta")
            return TryApplyDieFaceDeltaEffect(effect, context, value);
        if (effectType == "reroll_adventurer_dice")
            return TryApplyRerollAdventurerDiceEffect(effect, context);

        return false;
    }

    bool TryApplyStabilityDelta(int delta)
    {
        int current = RunState.stability;
        int next = current + delta;
        if (next < 0)
            next = 0;
        if (next > RunState.maxStability)
            next = RunState.maxStability;

        RunState.stability = next;
        return true;
    }

    bool TryApplyGoldDelta(int delta)
    {
        int current = RunState.gold;
        int next = current + delta;
        if (next < 0)
            next = 0;

        RunState.gold = next;
        return true;
    }

    bool TryApplyEnemyHealthDeltaEffect(EffectSpec effect, EffectTargetContext context, int delta)
    {
        string targetMode = GetParamString(effect.effectParams, "target_mode");
        string targetEnemyInstanceId = targetMode switch
        {
            "selected_enemy" => context.selectedEnemyInstanceId,
            "action_owner_enemy" => context.actionOwnerEnemyInstanceId,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(targetEnemyInstanceId))
            return false;
        if (FindEnemyState(targetEnemyInstanceId) == null)
            return false;

        ApplyEnemyHealthDelta(
            targetEnemyInstanceId,
            delta,
            rewardOnKill: true,
            markAssignedAsConsumedOnKill: false);
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

    bool ApplyEnemyHealthDelta(
        string enemyInstanceId,
        int delta,
        bool rewardOnKill,
        bool markAssignedAsConsumedOnKill)
    {
        var enemy = FindEnemyState(enemyInstanceId);
        if (enemy == null)
            return false;

        int next = enemy.currentHealth + delta;
        if (next < 0)
            next = 0;
        enemy.currentHealth = next;

        if (enemy.currentHealth > 0)
            return true;

        HandleEnemyKilled(enemy.instanceId, rewardOnKill, markAssignedAsConsumedOnKill);
        return false;
    }

    void HandleEnemyKilled(
        string enemyInstanceId,
        bool rewardOnKill,
        bool markAssignedAsConsumedOnKill)
    {
        var enemy = FindEnemyState(enemyInstanceId);
        if (enemy == null)
            return;

        var detachedAdventurerIds = new List<string>(enemy.assignedAdventurerIds);

        RemoveEnemyState(enemy.instanceId);

        for (int i = 0; i < detachedAdventurerIds.Count; i++)
        {
            string adventurerInstanceId = detachedAdventurerIds[i];
            var adventurer = FindAdventurerState(adventurerInstanceId);
            if (adventurer == null)
                continue;
            if (!string.Equals(adventurer.assignedEnemyInstanceId, enemyInstanceId, StringComparison.Ordinal))
                continue;

            adventurer.assignedEnemyInstanceId = null;
            if (markAssignedAsConsumedOnKill)
                adventurer.actionConsumed = true;

            AssignmentChanged?.Invoke(adventurer.instanceId, null);
        }

        if (rewardOnKill && enemyDefById.TryGetValue(enemy.enemyDefId, out var enemyDef))
        {
            var rewardContext = new EffectTargetContext(
                selectedEnemyInstanceId: enemy.instanceId,
                selectedAdventurerInstanceId: null,
                selectedDieIndex: -1,
                actionOwnerEnemyInstanceId: enemy.instanceId);
            TryApplyEffectBundle(enemyDef.onKillReward, rewardContext);
        }
    }

    void RemoveEnemyState(string enemyInstanceId)
    {
        for (int i = RunState.enemies.Count - 1; i >= 0; i--)
        {
            var enemy = RunState.enemies[i];
            if (enemy == null)
                continue;
            if (!string.Equals(enemy.instanceId, enemyInstanceId, StringComparison.Ordinal))
                continue;

            RunState.enemies.RemoveAt(i);
            break;
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

    EnemyState FindEnemyState(string enemyInstanceId)
    {
        for (int i = 0; i < RunState.enemies.Count; i++)
        {
            var enemy = RunState.enemies[i];
            if (enemy == null)
                continue;
            if (!string.Equals(enemy.instanceId, enemyInstanceId, StringComparison.Ordinal))
                continue;

            return enemy;
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
        enemyDefById = new Dictionary<string, EnemyDef>(StringComparer.Ordinal);
        adventurerDefById = new Dictionary<string, AdventurerDef>(StringComparer.Ordinal);
        skillDefById = new Dictionary<string, SkillDef>(StringComparer.Ordinal);

        if (dataSet?.enemyDefs != null)
        {
            for (int i = 0; i < dataSet.enemyDefs.Count; i++)
            {
                var def = dataSet.enemyDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.enemyId))
                    continue;

                enemyDefById[def.enemyId] = def;
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

    readonly struct EffectTargetContext
    {
        public readonly string selectedEnemyInstanceId;
        public readonly string selectedAdventurerInstanceId;
        public readonly int selectedDieIndex;
        public readonly string actionOwnerEnemyInstanceId;

        public EffectTargetContext(
            string selectedEnemyInstanceId,
            string selectedAdventurerInstanceId,
            int selectedDieIndex,
            string actionOwnerEnemyInstanceId)
        {
            this.selectedEnemyInstanceId = selectedEnemyInstanceId;
            this.selectedAdventurerInstanceId = selectedAdventurerInstanceId;
            this.selectedDieIndex = selectedDieIndex;
            this.actionOwnerEnemyInstanceId = actionOwnerEnemyInstanceId;
        }
    }
}
