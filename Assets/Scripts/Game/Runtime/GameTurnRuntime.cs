using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameTurnPhase
{
    None = 0,
    TurnStart = 1,
    Assignment = 2,
    Roll = 3,
    Adjustment = 4,
    Resolution = 5
}

public sealed class GameDieRuntimeState
{
    public int dieIndex { get; set; }
    public string upgradeId { get; set; } = string.Empty;
    public int? assignedSituationInstanceId { get; set; }
    public int rolledFace { get; set; }
    public int currentFace { get; set; }
    public bool hasRolled { get; set; }
}

public sealed class GameSituationRuntimeState
{
    public int situationInstanceId { get; set; }
    public string situationId { get; set; } = string.Empty;
    public int demand { get; set; }
    public int deadline { get; set; }
    public int boardOrder { get; set; }
}

public sealed class GameRunRuntimeState
{
    public int turnNumber { get; set; }
    public int defense { get; set; }
    public int stability { get; set; }
    public int gold { get; set; }
    public int waveRiskBudget { get; set; } = 2000;
    public int spawnPeriodTurns { get; set; } = 4;
    public GameTurnPhase phase { get; set; } = GameTurnPhase.None;
    public int lastResolutionAppliedDemandTotal { get; set; }
    public int lastResolutionSuccessCount { get; set; }
    public int lastResolutionFailCount { get; set; }
    public bool isGameOver { get; set; }
    public string gameOverReason { get; set; } = string.Empty;
    public Dictionary<string, int> resourceGuardUntilTurnByResource { get; set; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public List<GameDieRuntimeState> dice { get; set; } = new();
    public List<GameSituationRuntimeState> activeSituations { get; set; } = new();
}

public sealed class GameTurnRuntime
{
    sealed class QueuedEffectEntry
    {
        public GameEffectDefinition effect { get; set; }
        public int? sourceSituationInstanceId { get; set; }
        public int? selectedSituationInstanceId { get; set; }
        public int? selectedDieIndex { get; set; }
    }

    readonly GameStaticDataCatalog catalog;
    System.Random random;
    int nextSituationInstanceId = 1;
    bool isResolving;

    public GameRunRuntimeState state { get; private set; } = new();

    public event Action<GameTurnPhase> phaseChanged;

    public GameTurnRuntime(System.Random random, GameStaticDataCatalog catalog)
    {
        this.random = random ?? new System.Random();
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public void SetRandom(System.Random random)
    {
        this.random = random ?? new System.Random();
    }

    public void StartNewRun(GameStartingLoadout loadout)
    {
        if (loadout == null)
            throw new ArgumentNullException(nameof(loadout));

        nextSituationInstanceId = 1;
        isResolving = false;

        state = new GameRunRuntimeState
        {
            turnNumber = 0,
            defense = loadout.defense,
            stability = loadout.stability,
            gold = loadout.gold,
            phase = GameTurnPhase.None
        };

        state.dice.Clear();
        for (int dieIndex = 0; dieIndex < loadout.totalDiceCount; dieIndex++)
        {
            state.dice.Add(new GameDieRuntimeState
            {
                dieIndex = dieIndex
            });
        }

        for (int i = 0; i < loadout.diceUpgradeGrants.Count; i++)
        {
            var grant = loadout.diceUpgradeGrants[i];
            if (grant == null)
                continue;
            if (grant.diceIndex < 0 || grant.diceIndex >= state.dice.Count)
                continue;

            state.dice[grant.diceIndex].upgradeId = grant.upgradeId ?? string.Empty;
        }

        EnterTurnStart();
    }

    public bool TryAdvancePhase()
    {
        if (state.isGameOver)
            return false;

        switch (state.phase)
        {
            case GameTurnPhase.TurnStart:
                EnterAssignment();
                return true;
            case GameTurnPhase.Assignment:
                EnterRoll();
                return true;
            case GameTurnPhase.Roll:
                EnterAdjustment();
                return true;
            case GameTurnPhase.Adjustment:
                EnterResolution();
                return true;
            case GameTurnPhase.Resolution:
                EnterTurnStart();
                return true;
            default:
                return false;
        }
    }

    public bool TryAssignDieToSituation(int dieIndex, int? situationInstanceId)
    {
        if (state.isGameOver)
            return false;
        if (state.phase != GameTurnPhase.Assignment)
            return false;
        if (dieIndex < 0 || dieIndex >= state.dice.Count)
            return false;

        if (situationInstanceId.HasValue && !TryFindSituation(situationInstanceId.Value, out _))
            return false;

        state.dice[dieIndex].assignedSituationInstanceId = situationInstanceId;
        return true;
    }

    public bool TryRemoveSituation(int situationInstanceId)
    {
        if (state.isGameOver)
            return false;

        var index = state.activeSituations.FindIndex(item => item.situationInstanceId == situationInstanceId);
        if (index < 0)
            return false;

        RemoveSituationAt(index);
        return true;
    }

    public bool TryApplyDirectEffects(
        IReadOnlyList<GameEffectDefinition> effects,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        int? selectedDieIndex = null)
    {
        if (state.isGameOver)
            return false;
        if (effects == null || effects.Count == 0)
            return false;

        return EnqueueEffectListAndProcess(
            effects,
            sourceSituationInstanceId,
            selectedSituationInstanceId,
            selectedDieIndex);
    }

    void EnterTurnStart()
    {
        state.turnNumber += 1;
        state.phase = GameTurnPhase.TurnStart;
        RefillDiceForTurn();
        ApplyTurnStartEffects();

        if (!state.isGameOver)
            SpawnSituationsIfScheduled();

        phaseChanged?.Invoke(state.phase);
    }

    void EnterAssignment()
    {
        state.phase = GameTurnPhase.Assignment;
        phaseChanged?.Invoke(state.phase);
    }

    void EnterRoll()
    {
        state.phase = GameTurnPhase.Roll;
        RollAssignedDice();
        phaseChanged?.Invoke(state.phase);
    }

    void EnterAdjustment()
    {
        state.phase = GameTurnPhase.Adjustment;
        phaseChanged?.Invoke(state.phase);
    }

    void EnterResolution()
    {
        state.phase = GameTurnPhase.Resolution;
        ResolveCurrentTurn();
        phaseChanged?.Invoke(state.phase);
    }

    void RefillDiceForTurn()
    {
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            die.assignedSituationInstanceId = null;
            die.rolledFace = 0;
            die.currentFace = 0;
            die.hasRolled = false;
        }
    }

    void ApplyTurnStartEffects()
    {
        if (state.activeSituations.Count == 0)
            return;

        var orderedSituationIds = new List<int>(state.activeSituations.Count);
        for (int i = 0; i < state.activeSituations.Count; i++)
            orderedSituationIds.Add(state.activeSituations[i].situationInstanceId);

        for (int i = 0; i < orderedSituationIds.Count; i++)
        {
            if (state.isGameOver)
                return;

            var sourceSituationId = orderedSituationIds[i];
            if (!TryFindSituation(sourceSituationId, out var sourceSituation))
                continue;
            if (!catalog.TryGetSituation(sourceSituation.situationId, out var situationDefinition))
                continue;
            if (situationDefinition.onTurnStartEffects == null || situationDefinition.onTurnStartEffects.Count == 0)
                continue;

            EnqueueEffectListAndProcess(
                situationDefinition.onTurnStartEffects,
                sourceSituationId,
                null,
                null);
        }
    }

    void RollAssignedDice()
    {
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.assignedSituationInstanceId.HasValue)
                continue;

            var rolledFace = random.Next(1, 7);
            die.rolledFace = rolledFace;
            die.currentFace = rolledFace;
            die.hasRolled = true;

            ApplyRollOnceUpgradeIfNeeded(die);
            ApplyRecheckUpgradeIfNeeded(die, true);
        }
    }

    void SpawnSituationsIfScheduled()
    {
        if (catalog.situations.Count == 0)
            return;
        if (!IsSpawnTurn(state.turnNumber, state.spawnPeriodTurns))
            return;

        var cumulativeRisk = 0f;
        var spawnedSituationIds = new List<string>();
        while (true)
        {
            var randomIndex = random.Next(0, catalog.situations.Count);
            var definition = catalog.situations[randomIndex];
            var nextRisk = cumulativeRisk + definition.riskValue;
            if (nextRisk > state.waveRiskBudget)
                break;

            AddSituation(definition);
            spawnedSituationIds.Add(definition.situationId);
            cumulativeRisk = nextRisk;
        }

        Debug.Log($"[GameTurnRuntime] turn={state.turnNumber} spawn budget={state.waveRiskBudget} used={cumulativeRisk:0.##} count={spawnedSituationIds.Count} ids=[{string.Join(",", spawnedSituationIds)}]");
    }

    void AddSituation(GameSituationDefinition definition)
    {
        var situation = new GameSituationRuntimeState
        {
            situationInstanceId = nextSituationInstanceId++,
            situationId = definition.situationId,
            demand = definition.demand,
            deadline = definition.deadline
        };

        state.activeSituations.Add(situation);
        ReindexBoardOrder();
    }

    void ReindexBoardOrder()
    {
        for (int i = 0; i < state.activeSituations.Count; i++)
            state.activeSituations[i].boardOrder = i;
    }

    void ResolveCurrentTurn()
    {
        state.lastResolutionAppliedDemandTotal = 0;
        state.lastResolutionSuccessCount = 0;
        state.lastResolutionFailCount = 0;
        isResolving = true;

        ApplyDemandByAssignedDice();
        ApplyDeadlineTickAndFail();

        isResolving = false;
        Debug.Log($"[GameTurnRuntime] turn={state.turnNumber} resolution demandApplied={state.lastResolutionAppliedDemandTotal} success={state.lastResolutionSuccessCount} fail={state.lastResolutionFailCount} remain={state.activeSituations.Count}");
    }

    void ApplyDemandByAssignedDice()
    {
        int index = 0;
        while (index < state.activeSituations.Count)
        {
            var situation = state.activeSituations[index];
            var assignedDemand = GetAssignedDiceDemand(situation.situationInstanceId);
            if (assignedDemand > 0)
            {
                state.lastResolutionAppliedDemandTotal += assignedDemand;
                ApplyDemandDeltaAndProcess(
                    situation.situationInstanceId,
                    -assignedDemand,
                    null,
                    null);
            }

            if (TryFindSituation(situation.situationInstanceId, out _))
                index += 1;
        }
    }

    void ApplyDeadlineTickAndFail()
    {
        int index = 0;
        while (index < state.activeSituations.Count)
        {
            var situation = state.activeSituations[index];
            situation.deadline -= 1;
            if (situation.deadline <= 0)
            {
                ResolveSituationFailAndProcess(
                    situation.situationInstanceId,
                    null,
                    null);
                continue;
            }

            index += 1;
        }
    }

    int GetAssignedDiceDemand(int situationInstanceId)
    {
        int demand = 0;
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.hasRolled)
                continue;
            if (!die.assignedSituationInstanceId.HasValue || die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;

            demand += Mathf.Max(0, die.currentFace);
        }

        return demand;
    }

    void RemoveSituationAt(int index)
    {
        if (index < 0 || index >= state.activeSituations.Count)
            return;

        var removedSituationId = state.activeSituations[index].situationInstanceId;
        state.activeSituations.RemoveAt(index);

        for (int i = 0; i < state.dice.Count; i++)
        {
            if (state.dice[i].assignedSituationInstanceId.HasValue &&
                state.dice[i].assignedSituationInstanceId.Value == removedSituationId)
            {
                state.dice[i].assignedSituationInstanceId = null;
            }
        }

        ReindexBoardOrder();
    }

    void ApplyDemandDeltaAndProcess(
        int situationInstanceId,
        int delta,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId)
    {
        var queue = new Queue<QueuedEffectEntry>();
        ApplyDemandDeltaCore(
            situationInstanceId,
            delta,
            sourceSituationInstanceId,
            selectedSituationInstanceId,
            queue);
        ProcessQueuedEffects(queue);
    }

    void ResolveSituationFailAndProcess(
        int situationInstanceId,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId)
    {
        var queue = new Queue<QueuedEffectEntry>();
        ResolveSituationFailCore(
            situationInstanceId,
            sourceSituationInstanceId,
            selectedSituationInstanceId,
            queue);
        ProcessQueuedEffects(queue);
    }

    bool EnqueueEffectListAndProcess(
        IReadOnlyList<GameEffectDefinition> effects,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        int? selectedDieIndex)
    {
        var queue = new Queue<QueuedEffectEntry>();
        EnqueueEffectList(
            queue,
            effects,
            sourceSituationInstanceId,
            selectedSituationInstanceId,
            selectedDieIndex);
        return ProcessQueuedEffects(queue);
    }

    bool ProcessQueuedEffects(Queue<QueuedEffectEntry> queue)
    {
        if (queue == null)
            return false;

        const int maxIterations = 10000;
        var iterations = 0;
        var anyApplied = false;
        while (queue.Count > 0)
        {
            iterations += 1;
            if (iterations > maxIterations)
            {
                Debug.LogError("[GameTurnRuntime] Effect processing aborted: max iterations exceeded.");
                return anyApplied;
            }

            if (state.isGameOver)
                return anyApplied;

            var entry = queue.Dequeue();
            if (ApplyEffectEntry(entry, queue))
                anyApplied = true;
        }

        return anyApplied;
    }

    bool ApplyEffectEntry(QueuedEffectEntry entry, Queue<QueuedEffectEntry> queue)
    {
        if (entry == null || entry.effect == null)
            return false;

        var effect = entry.effect;
        switch (effect.effectType)
        {
            case "resource_delta":
                return ApplyResourceDelta(effect);
            case "gold_delta":
                return ApplyGoldDelta(effect);
            case "demand_delta":
                return ApplyDemandDeltaEffect(effect, entry.sourceSituationInstanceId, entry.selectedSituationInstanceId, queue);
            case "deadline_delta":
                return ApplyDeadlineDeltaEffect(effect, entry.sourceSituationInstanceId, entry.selectedSituationInstanceId, queue);
            case "resource_guard":
                return ApplyResourceGuard(effect);
            case "die_face_delta":
                return ApplyDieFaceDeltaEffect(effect, entry.selectedSituationInstanceId, entry.selectedDieIndex);
            case "die_face_set":
                return ApplyDieFaceSetEffect(effect, entry.selectedSituationInstanceId, entry.selectedDieIndex);
            case "die_face_min":
                return ApplyDieFaceMinEffect(effect, entry.selectedSituationInstanceId, entry.selectedDieIndex);
            case "die_face_mult":
                return ApplyDieFaceMultEffect(effect, entry.selectedSituationInstanceId, entry.selectedDieIndex);
            case "reroll_assigned_dice":
                return ApplyRerollAssignedDiceEffect(effect, entry.selectedSituationInstanceId);
            default:
                return false;
        }
    }

    bool ApplyResourceDelta(GameEffectDefinition effect)
    {
        if (!effect.value.HasValue)
            return false;

        var target = effect.targetResource ?? string.Empty;
        var delta = Mathf.RoundToInt(effect.value.Value);
        if (delta == 0)
            return false;

        if (delta < 0 && IsResourceGuardActive(target))
            return false;

        if (target.Equals("defense", StringComparison.OrdinalIgnoreCase))
            state.defense += delta;
        else if (target.Equals("stability", StringComparison.OrdinalIgnoreCase))
            state.stability += delta;
        else
            return false;

        CheckGameOver();
        return true;
    }

    bool ApplyGoldDelta(GameEffectDefinition effect)
    {
        if (!effect.value.HasValue)
            return false;

        var delta = Mathf.RoundToInt(effect.value.Value);
        if (delta == 0)
            return false;

        state.gold += delta;
        return true;
    }

    bool ApplyResourceGuard(GameEffectDefinition effect)
    {
        if (!effect.duration.HasValue || effect.duration.Value <= 0)
            return false;

        var target = effect.targetResource ?? string.Empty;
        if (!target.Equals("defense", StringComparison.OrdinalIgnoreCase) &&
            !target.Equals("stability", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var untilTurn = state.turnNumber + effect.duration.Value - 1;
        if (state.resourceGuardUntilTurnByResource.TryGetValue(target, out var existingUntilTurn))
            state.resourceGuardUntilTurnByResource[target] = Mathf.Max(existingUntilTurn, untilTurn);
        else
            state.resourceGuardUntilTurnByResource[target] = untilTurn;

        return true;
    }

    bool IsResourceGuardActive(string targetResource)
    {
        if (string.IsNullOrWhiteSpace(targetResource))
            return false;
        if (!state.resourceGuardUntilTurnByResource.TryGetValue(targetResource, out var untilTurn))
            return false;

        return state.turnNumber <= untilTurn;
    }

    bool ApplyDemandDeltaEffect(
        GameEffectDefinition effect,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        Queue<QueuedEffectEntry> queue)
    {
        if (!effect.value.HasValue)
            return false;

        var delta = Mathf.RoundToInt(effect.value.Value);
        if (delta == 0)
            return false;

        var anyApplied = false;
        var targets = ResolveSituationTargets(effect, sourceSituationInstanceId, selectedSituationInstanceId);
        for (int i = 0; i < targets.Count; i++)
        {
            if (ApplyDemandDeltaCore(
                targets[i],
                delta,
                sourceSituationInstanceId,
                selectedSituationInstanceId,
                queue))
            {
                anyApplied = true;
            }

            if (ProcessQueuedEffects(queue))
                anyApplied = true;

            if (state.isGameOver)
                return anyApplied;
        }

        return anyApplied;
    }

    bool ApplyDeadlineDeltaEffect(
        GameEffectDefinition effect,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        Queue<QueuedEffectEntry> queue)
    {
        if (!effect.value.HasValue)
            return false;

        var delta = Mathf.RoundToInt(effect.value.Value);
        if (delta == 0)
            return false;

        var anyApplied = false;
        var targets = ResolveSituationTargets(effect, sourceSituationInstanceId, selectedSituationInstanceId);
        for (int i = 0; i < targets.Count; i++)
        {
            if (ApplyDeadlineDeltaCore(
                targets[i],
                delta,
                sourceSituationInstanceId,
                selectedSituationInstanceId,
                queue))
            {
                anyApplied = true;
            }

            if (ProcessQueuedEffects(queue))
                anyApplied = true;

            if (state.isGameOver)
                return anyApplied;
        }

        return anyApplied;
    }

    bool ApplyDieFaceDeltaEffect(
        GameEffectDefinition effect,
        int? selectedSituationInstanceId,
        int? selectedDieIndex)
    {
        if (!effect.value.HasValue)
            return false;

        var value = Mathf.RoundToInt(effect.value.Value);
        if (value == 0)
            return false;

        var mode = effect.targetMode ?? string.Empty;
        if (mode.Equals("selected_die", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(mode))
        {
            if (!selectedDieIndex.HasValue)
                return false;

            return ApplyDieFaceDeltaByIndex(selectedDieIndex.Value, value);
        }

        if (mode.Equals("assigned_dice_in_selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedSituationInstanceId.HasValue)
                return false;

            return ApplyDieFaceDeltaInSituation(selectedSituationInstanceId.Value, value);
        }

        return false;
    }

    bool ApplyDieFaceSetEffect(
        GameEffectDefinition effect,
        int? selectedSituationInstanceId,
        int? selectedDieIndex)
    {
        if (!effect.value.HasValue)
            return false;

        var value = Mathf.RoundToInt(effect.value.Value);
        var mode = effect.targetMode ?? string.Empty;
        if (mode.Equals("selected_die", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(mode))
        {
            if (!selectedDieIndex.HasValue)
                return false;

            return SetDieFaceByIndex(selectedDieIndex.Value, value);
        }

        if (mode.Equals("assigned_dice_in_selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedSituationInstanceId.HasValue)
                return false;

            return SetDieFaceInSituation(selectedSituationInstanceId.Value, value);
        }

        return false;
    }

    bool ApplyDieFaceMinEffect(
        GameEffectDefinition effect,
        int? selectedSituationInstanceId,
        int? selectedDieIndex)
    {
        if (!effect.value.HasValue)
            return false;

        var minValue = Mathf.RoundToInt(effect.value.Value);
        var mode = effect.targetMode ?? string.Empty;
        if (mode.Equals("selected_die", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(mode))
        {
            if (!selectedDieIndex.HasValue)
                return false;
            if (!TryGetDieByIndex(selectedDieIndex.Value, out var die))
                return false;

            var next = Mathf.Max(die.currentFace, minValue);
            return SetDieFaceByIndex(selectedDieIndex.Value, next);
        }

        if (mode.Equals("assigned_dice_in_selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedSituationInstanceId.HasValue)
                return false;

            var anyApplied = false;
            for (int i = 0; i < state.dice.Count; i++)
            {
                var die = state.dice[i];
                if (!die.hasRolled)
                    continue;
                if (!die.assignedSituationInstanceId.HasValue || die.assignedSituationInstanceId.Value != selectedSituationInstanceId.Value)
                    continue;

                var next = Mathf.Max(die.currentFace, minValue);
                if (SetDieFaceByIndex(die.dieIndex, next))
                    anyApplied = true;
            }

            return anyApplied;
        }

        return false;
    }

    bool ApplyDieFaceMultEffect(
        GameEffectDefinition effect,
        int? selectedSituationInstanceId,
        int? selectedDieIndex)
    {
        if (!effect.value.HasValue)
            return false;

        var multiplier = effect.value.Value;
        if (Mathf.Approximately(multiplier, 1f))
            return false;

        var mode = effect.targetMode ?? string.Empty;
        if (mode.Equals("selected_die", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(mode))
        {
            if (!selectedDieIndex.HasValue)
                return false;
            if (!TryGetDieByIndex(selectedDieIndex.Value, out var die))
                return false;

            var next = Mathf.RoundToInt(die.currentFace * multiplier);
            return SetDieFaceByIndex(selectedDieIndex.Value, next);
        }

        if (mode.Equals("assigned_dice_in_selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedSituationInstanceId.HasValue)
                return false;

            var anyApplied = false;
            for (int i = 0; i < state.dice.Count; i++)
            {
                var die = state.dice[i];
                if (!die.hasRolled)
                    continue;
                if (!die.assignedSituationInstanceId.HasValue || die.assignedSituationInstanceId.Value != selectedSituationInstanceId.Value)
                    continue;

                var next = Mathf.RoundToInt(die.currentFace * multiplier);
                if (SetDieFaceByIndex(die.dieIndex, next))
                    anyApplied = true;
            }

            return anyApplied;
        }

        return false;
    }

    bool ApplyRerollAssignedDiceEffect(GameEffectDefinition effect, int? selectedSituationInstanceId)
    {
        var mode = effect.targetMode ?? string.Empty;
        if (!mode.Equals("selected_situation", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!selectedSituationInstanceId.HasValue)
            return false;

        return RerollAssignedDiceInSituation(selectedSituationInstanceId.Value);
    }

    bool ApplyDieFaceDeltaByIndex(int dieIndex, int delta)
    {
        if (delta == 0)
            return false;
        if (!TryGetDieByIndex(dieIndex, out var die))
            return false;
        if (!die.hasRolled)
            return false;

        return SetDieFaceByIndex(dieIndex, die.currentFace + delta);
    }

    bool ApplyDieFaceDeltaInSituation(int situationInstanceId, int delta)
    {
        if (delta == 0)
            return false;
        if (!TryFindSituation(situationInstanceId, out _))
            return false;

        var anyApplied = false;
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.hasRolled)
                continue;
            if (!die.assignedSituationInstanceId.HasValue || die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;

            if (SetDieFaceByIndex(die.dieIndex, die.currentFace + delta))
                anyApplied = true;
        }

        return anyApplied;
    }

    bool SetDieFaceInSituation(int situationInstanceId, int value)
    {
        if (!TryFindSituation(situationInstanceId, out _))
            return false;

        var anyApplied = false;
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.hasRolled)
                continue;
            if (!die.assignedSituationInstanceId.HasValue || die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;

            if (SetDieFaceByIndex(die.dieIndex, value))
                anyApplied = true;
        }

        return anyApplied;
    }

    bool RerollAssignedDiceInSituation(int situationInstanceId)
    {
        if (!TryFindSituation(situationInstanceId, out _))
            return false;

        var anyApplied = false;
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.hasRolled)
                continue;
            if (!die.assignedSituationInstanceId.HasValue || die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;

            var before = die.currentFace;
            var rerolled = random.Next(1, 7);
            die.rolledFace = rerolled;
            die.currentFace = rerolled;
            if (ApplyRecheckUpgradeIfNeeded(die, true))
                anyApplied = true;
            if (die.currentFace != before)
                anyApplied = true;
        }

        return anyApplied;
    }

    bool SetDieFaceByIndex(int dieIndex, int value)
    {
        if (!TryGetDieByIndex(dieIndex, out var die))
            return false;
        if (!die.hasRolled)
            return false;

        var clamped = Mathf.Max(0, value);
        var changed = die.currentFace != clamped;
        die.currentFace = clamped;
        if (ApplyRecheckUpgradeIfNeeded(die, false))
            changed = true;

        return changed;
    }

    bool TryGetDieByIndex(int dieIndex, out GameDieRuntimeState die)
    {
        if (dieIndex < 0 || dieIndex >= state.dice.Count)
        {
            die = null;
            return false;
        }

        die = state.dice[dieIndex];
        return die != null;
    }

    bool ApplyRollOnceUpgradeIfNeeded(GameDieRuntimeState die)
    {
        if (die == null || string.IsNullOrWhiteSpace(die.upgradeId))
            return false;
        if (!catalog.TryGetDiceUpgrade(die.upgradeId, out var upgrade))
            return false;
        if (!"roll_once_after_roll".Equals(upgrade.triggerType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!EvaluateDiceUpgradeCondition(upgrade, die))
            return false;

        var changed = false;
        for (int i = 0; i < upgrade.effects.Count; i++)
        {
            var effect = upgrade.effects[i];
            if (effect == null)
                continue;
            if (ApplyDiceUpgradeEffectToDie(effect, die))
                changed = true;
        }

        return changed;
    }

    bool ApplyRecheckUpgradeIfNeeded(GameDieRuntimeState die, bool isInitialRoll)
    {
        if (die == null || string.IsNullOrWhiteSpace(die.upgradeId))
            return false;
        if (!catalog.TryGetDiceUpgrade(die.upgradeId, out var upgrade))
            return false;
        if (!"recheck_on_die_face_change".Equals(upgrade.triggerType, StringComparison.OrdinalIgnoreCase))
            return false;

        var anyChanged = false;
        var guard = 0;
        while (guard < 24)
        {
            guard += 1;
            if (!EvaluateDiceUpgradeCondition(upgrade, die))
                break;

            var changedThisIteration = false;
            for (int i = 0; i < upgrade.effects.Count; i++)
            {
                var effect = upgrade.effects[i];
                if (effect == null)
                    continue;
                if (ApplyDiceUpgradeEffectToDie(effect, die))
                    changedThisIteration = true;
            }

            if (!changedThisIteration)
                break;

            anyChanged = true;
        }

        return anyChanged;
    }

    bool EvaluateDiceUpgradeCondition(GameDiceUpgradeDefinition upgrade, GameDieRuntimeState die)
    {
        if (upgrade == null)
            return false;
        if (die == null)
            return false;
        if (upgrade.conditions == null || upgrade.conditions.Count == 0)
            return true;

        return EvaluateDiceCondition(upgrade.conditions[0], die);
    }

    bool EvaluateDiceCondition(GameConditionDefinition condition, GameDieRuntimeState die)
    {
        if (condition == null || string.IsNullOrWhiteSpace(condition.conditionType))
            return true;

        var value = condition.value ?? 0f;
        var type = condition.conditionType;
        switch (type)
        {
            case "assigned_situation_deadline_lte":
                if (!die.assignedSituationInstanceId.HasValue)
                    return false;
                if (!TryFindSituation(die.assignedSituationInstanceId.Value, out var situationByDeadline))
                    return false;
                return situationByDeadline.deadline <= value;
            case "die_face_lte":
                return die.currentFace <= value;
            case "die_face_eq":
                return Mathf.Approximately(die.currentFace, value);
            case "die_face_gte":
                return die.currentFace >= value;
            case "assigned_dice_count_in_situation_gte":
                if (!die.assignedSituationInstanceId.HasValue)
                    return false;
                return CountAssignedDiceInSituation(die.assignedSituationInstanceId.Value) >= value;
            case "assigned_dice_count_in_situation_lte":
                if (!die.assignedSituationInstanceId.HasValue)
                    return false;
                return CountAssignedDiceInSituation(die.assignedSituationInstanceId.Value) <= value;
            case "player_defense_lte":
                return state.defense <= value;
            case "player_defense_gte":
                return state.defense >= value;
            case "player_stability_lte":
                return state.stability <= value;
            case "player_stability_gte":
                return state.stability >= value;
            case "board_order_first":
                if (!die.assignedSituationInstanceId.HasValue)
                    return false;
                if (!TryFindSituation(die.assignedSituationInstanceId.Value, out var firstSituation))
                    return false;
                return firstSituation.boardOrder == 0;
            case "board_order_last":
                if (!die.assignedSituationInstanceId.HasValue)
                    return false;
                if (!TryFindSituation(die.assignedSituationInstanceId.Value, out var lastSituation))
                    return false;
                return lastSituation.boardOrder == state.activeSituations.Count - 1;
            case "on_resolve_success":
                return false;
            default:
                return false;
        }
    }

    int CountAssignedDiceInSituation(int situationInstanceId)
    {
        var count = 0;
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.assignedSituationInstanceId.HasValue)
                continue;
            if (die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;
            count += 1;
        }

        return count;
    }

    bool ApplyDiceUpgradeEffectToDie(GameEffectDefinition effect, GameDieRuntimeState die)
    {
        if (effect == null || die == null)
            return false;

        switch (effect.effectType)
        {
            case "die_face_delta":
            {
                if (!effect.value.HasValue)
                    return false;
                var delta = Mathf.RoundToInt(effect.value.Value);
                if (delta == 0)
                    return false;
                var next = Mathf.Max(0, die.currentFace + delta);
                if (next == die.currentFace)
                    return false;
                die.currentFace = next;
                return true;
            }
            case "die_face_set":
            {
                if (!effect.value.HasValue)
                    return false;
                var setValue = Mathf.Max(0, Mathf.RoundToInt(effect.value.Value));
                if (setValue == die.currentFace)
                    return false;
                die.currentFace = setValue;
                return true;
            }
            case "die_face_min":
            {
                if (!effect.value.HasValue)
                    return false;
                var minValue = Mathf.Max(0, Mathf.RoundToInt(effect.value.Value));
                var next = Mathf.Max(die.currentFace, minValue);
                if (next == die.currentFace)
                    return false;
                die.currentFace = next;
                return true;
            }
            case "die_face_mult":
            {
                if (!effect.value.HasValue)
                    return false;
                var next = Mathf.Max(0, Mathf.RoundToInt(die.currentFace * effect.value.Value));
                if (next == die.currentFace)
                    return false;
                die.currentFace = next;
                return true;
            }
            default:
                return false;
        }
    }

    bool ApplyDemandDeltaCore(
        int situationInstanceId,
        int delta,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        Queue<QueuedEffectEntry> queue)
    {
        if (delta == 0)
            return false;
        if (!TryFindSituation(situationInstanceId, out var situation))
            return false;

        situation.demand += delta;
        var changed = true;
        if (situation.demand <= 0)
        {
            ResolveSituationSuccessCore(
                situationInstanceId,
                sourceSituationInstanceId,
                selectedSituationInstanceId,
                queue);
        }

        return changed;
    }

    bool ApplyDeadlineDeltaCore(
        int situationInstanceId,
        int delta,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        Queue<QueuedEffectEntry> queue)
    {
        if (delta == 0)
            return false;
        if (!TryFindSituation(situationInstanceId, out var situation))
            return false;

        situation.deadline += delta;
        var changed = true;
        if (situation.deadline <= 0)
        {
            ResolveSituationFailCore(
                situationInstanceId,
                sourceSituationInstanceId,
                selectedSituationInstanceId,
                queue);
        }

        return changed;
    }

    void ResolveSituationSuccessCore(
        int situationInstanceId,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        Queue<QueuedEffectEntry> queue)
    {
        if (!TryFindSituation(situationInstanceId, out var situation))
            return;

        var hasDefinition = catalog.TryGetSituation(situation.situationId, out var situationDefinition);
        RemoveSituationByInstanceId(situationInstanceId);

        if (isResolving)
            state.lastResolutionSuccessCount += 1;

        if (!hasDefinition || situationDefinition.onSuccess == null || situationDefinition.onSuccess.Count == 0)
            return;

        EnqueueEffectList(
            queue,
            situationDefinition.onSuccess,
            situationInstanceId,
            selectedSituationInstanceId,
            null);
    }

    void ResolveSituationFailCore(
        int situationInstanceId,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        Queue<QueuedEffectEntry> queue)
    {
        if (!TryFindSituation(situationInstanceId, out var situation))
            return;

        var hasDefinition = catalog.TryGetSituation(situation.situationId, out var situationDefinition);
        RemoveSituationByInstanceId(situationInstanceId);

        if (isResolving)
            state.lastResolutionFailCount += 1;

        if (!hasDefinition || situationDefinition.onFail == null || situationDefinition.onFail.Count == 0)
            return;

        EnqueueEffectList(
            queue,
            situationDefinition.onFail,
            situationInstanceId,
            selectedSituationInstanceId,
            null);
    }

    void EnqueueEffectList(
        Queue<QueuedEffectEntry> queue,
        IReadOnlyList<GameEffectDefinition> effects,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId,
        int? selectedDieIndex)
    {
        if (queue == null || effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            queue.Enqueue(new QueuedEffectEntry
            {
                effect = effect,
                sourceSituationInstanceId = sourceSituationInstanceId,
                selectedSituationInstanceId = selectedSituationInstanceId,
                selectedDieIndex = selectedDieIndex
            });
        }
    }

    List<int> ResolveSituationTargets(
        GameEffectDefinition effect,
        int? sourceSituationInstanceId,
        int? selectedSituationInstanceId)
    {
        var targets = new List<int>();
        if (effect == null)
            return targets;

        var mode = effect.targetMode ?? string.Empty;
        if (mode.Equals("self", StringComparison.OrdinalIgnoreCase))
        {
            if (sourceSituationInstanceId.HasValue &&
                TryFindSituation(sourceSituationInstanceId.Value, out _))
            {
                targets.Add(sourceSituationInstanceId.Value);
            }

            return targets;
        }

        if (mode.Equals("selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (selectedSituationInstanceId.HasValue &&
                TryFindSituation(selectedSituationInstanceId.Value, out _))
            {
                targets.Add(selectedSituationInstanceId.Value);
            }

            return targets;
        }

        if (mode.Equals("all_other_situations", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < state.activeSituations.Count; i++)
            {
                var candidate = state.activeSituations[i];
                if (sourceSituationInstanceId.HasValue &&
                    candidate.situationInstanceId == sourceSituationInstanceId.Value)
                {
                    continue;
                }

                targets.Add(candidate.situationInstanceId);
            }

            return targets;
        }

        if (mode.Equals("random_other_situation", StringComparison.OrdinalIgnoreCase))
        {
            var candidates = new List<int>();
            for (int i = 0; i < state.activeSituations.Count; i++)
            {
                var candidate = state.activeSituations[i];
                if (sourceSituationInstanceId.HasValue &&
                    candidate.situationInstanceId == sourceSituationInstanceId.Value)
                {
                    continue;
                }

                candidates.Add(candidate.situationInstanceId);
            }

            if (candidates.Count == 0)
                return targets;

            var randomIndex = random.Next(0, candidates.Count);
            targets.Add(candidates[randomIndex]);
            return targets;
        }

        if (mode.Equals("by_tag", StringComparison.OrdinalIgnoreCase))
        {
            var targetTag = effect.targetTag ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetTag))
                return targets;

            for (int i = 0; i < state.activeSituations.Count; i++)
            {
                var candidate = state.activeSituations[i];
                if (!catalog.TryGetSituation(candidate.situationId, out var definition))
                    continue;
                if (definition.tags == null)
                    continue;

                for (int tagIndex = 0; tagIndex < definition.tags.Count; tagIndex++)
                {
                    var tag = definition.tags[tagIndex];
                    if (!targetTag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                        continue;

                    targets.Add(candidate.situationInstanceId);
                    break;
                }
            }

            return targets;
        }

        return targets;
    }

    void RemoveSituationByInstanceId(int situationInstanceId)
    {
        var index = state.activeSituations.FindIndex(item => item.situationInstanceId == situationInstanceId);
        if (index < 0)
            return;

        RemoveSituationAt(index);
    }

    void CheckGameOver()
    {
        if (state.isGameOver)
            return;

        var defenseDown = state.defense <= 0;
        var stabilityDown = state.stability <= 0;
        if (!defenseDown && !stabilityDown)
            return;

        state.isGameOver = true;
        if (defenseDown && stabilityDown)
            state.gameOverReason = "defense_and_stability_depleted";
        else if (defenseDown)
            state.gameOverReason = "defense_depleted";
        else
            state.gameOverReason = "stability_depleted";

        Debug.Log($"[GameTurnRuntime] game over. reason={state.gameOverReason}, turn={state.turnNumber}, defense={state.defense}, stability={state.stability}");
    }

    bool TryFindSituation(int situationInstanceId, out GameSituationRuntimeState situation)
    {
        for (int i = 0; i < state.activeSituations.Count; i++)
        {
            if (state.activeSituations[i].situationInstanceId == situationInstanceId)
            {
                situation = state.activeSituations[i];
                return true;
            }
        }

        situation = null;
        return false;
    }

    static bool IsSpawnTurn(int turnNumber, int spawnPeriodTurns)
    {
        if (turnNumber <= 0)
            return false;
        if (spawnPeriodTurns <= 0)
            return false;

        return ((turnNumber - 1) % spawnPeriodTurns) == 0;
    }
}
