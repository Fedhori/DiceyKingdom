using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameTurnOrchestrator : MonoBehaviour
{
    [SerializeField] bool autoStartOnAwake = true;
    [SerializeField] bool useFixedSeed;
    [SerializeField] int fixedSeed = 1001;

    GameStaticDataSet staticData;
    Dictionary<string, SituationDef> situationDefById;
    Dictionary<string, AgentDef> agentDefById;
    System.Random rng;
    bool isRunOver;
    bool isDuelResolutionPending;
    bool duelPresentationCompleted;

    public GameRunState RunState { get; private set; }

    public event Action<GameRunState> RunStarted;
    public event Action<TurnPhase> PhaseChanged;
    public event Action<GameRunState> RunEnded;
    public event Action<int, string> StageSpawned;
    public event Action<int> AssignmentCommitConfirmationRequested;
    public event Action StateChanged;
    public event Action<DuelRollPresentation> DuelRollStarted;

    public readonly struct DuelRollPresentation
    {
        public readonly string agentInstanceId;
        public readonly int agentDieIndex;
        public readonly int agentDieFace;
        public readonly int agentRoll;
        public readonly string situationInstanceId;
        public readonly int situationDieIndex;
        public readonly int situationDieFace;
        public readonly int situationRoll;
        public readonly bool success;

        public DuelRollPresentation(
            string agentInstanceId,
            int agentDieIndex,
            int agentDieFace,
            int agentRoll,
            string situationInstanceId,
            int situationDieIndex,
            int situationDieFace,
            int situationRoll,
            bool success)
        {
            this.agentInstanceId = agentInstanceId ?? string.Empty;
            this.agentDieIndex = agentDieIndex;
            this.agentDieFace = agentDieFace;
            this.agentRoll = agentRoll;
            this.situationInstanceId = situationInstanceId ?? string.Empty;
            this.situationDieIndex = situationDieIndex;
            this.situationDieFace = situationDieFace;
            this.situationRoll = situationRoll;
            this.success = success;
        }
    }

    void Awake()
    {
        if (!autoStartOnAwake)
            return;

        StartNewRun();
    }

    public void StartNewRun(int? seedOverride = null)
    {
        StopAllCoroutines();

        int seed = seedOverride ?? (useFixedSeed ? fixedSeed : Environment.TickCount);

        staticData = GameStaticDataLoader.LoadAll();
        BuildDefinitionLookups(staticData);
        rng = new System.Random(seed);
        RunState = GameRunBootstrap.CreateNewRun(staticData, seed);
        isRunOver = false;
        isDuelResolutionPending = false;
        duelPresentationCompleted = false;

        RunStarted?.Invoke(RunState);
        StageSpawned?.Invoke(RunState.stage.stageNumber, RunState.stage.activePresetId);

        AdvanceToDecisionPoint();
    }

    public bool CommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;
        if (GetPendingAgentCount() > 0)
            return false;

        RunState.turn.processingAgentInstanceId = string.Empty;
        RunState.turn.selectedAgentDieIndex = -1;
        SetPhase(TurnPhase.Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public bool TryRollAgentBySlotIndex(int slotIndex)
    {
        if (RunState?.agents == null)
            return false;
        if (slotIndex < 0 || slotIndex >= RunState.agents.Count)
            return false;

        var agent = RunState.agents[slotIndex];
        if (agent == null)
            return false;

        return TryRollAgent(agent.instanceId);
    }

    // Legacy name kept for UI wiring. In the new loop this means "select agent to spend dice".
    public bool TryRollAgent(string agentInstanceId)
    {
        if (!CanAcceptAgentSelectionInput())
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        var agent = FindAgentState(agentInstanceId);
        if (agent == null || agent.actionConsumed)
            return false;
        if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return false;

        RunState.turn.processingAgentInstanceId = agent.instanceId;
        RunState.turn.selectedAgentDieIndex = -1;
        SetPhase(TurnPhase.Adjustment);
        return true;
    }

    public bool TrySelectProcessingAgentDie(int dieIndex)
    {
        if (!CanAcceptAgentDieSelectionInput())
            return false;

        var agent = FindCurrentProcessingAgent();
        if (!IsValidAgentDieIndex(agent, dieIndex))
            return false;

        RunState.turn.selectedAgentDieIndex = dieIndex;
        SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    public bool TryTestAgainstSituationDie(string situationInstanceId, int situationDieIndex)
    {
        if (!CanAcceptDuelTargetingInput())
            return false;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return false;

        var agent = FindCurrentProcessingAgent();
        if (agent == null)
            return false;

        int selectedAgentDieIndex = RunState.turn.selectedAgentDieIndex;
        if (!IsValidAgentDieIndex(agent, selectedAgentDieIndex))
            return false;

        var situation = FindSituationState(situationInstanceId);
        if (!IsValidSituationDieIndex(situation, situationDieIndex))
            return false;

        int agentDieFace = Math.Max(1, agent.remainingDiceFaces[selectedAgentDieIndex]);
        int situationDieFace = Math.Max(1, situation.remainingDiceFaces[situationDieIndex]);

        int agentRoll = RollByFace(agentDieFace);
        int situationRoll = RollByFace(situationDieFace);
        bool success = agentRoll >= situationRoll;

        var duelPresentation = new DuelRollPresentation(
            agent.instanceId,
            selectedAgentDieIndex,
            agentDieFace,
            agentRoll,
            situation.instanceId,
            situationDieIndex,
            situationDieFace,
            situationRoll,
            success);

        isDuelResolutionPending = true;
        duelPresentationCompleted = false;
        DuelRollStarted?.Invoke(duelPresentation);
        StartCoroutine(ResolveDuelAfterPresentation(duelPresentation));
        return true;
    }

    public bool TryBeginAgentTargeting(string agentInstanceId)
    {
        if (!string.Equals(RunState?.turn.processingAgentInstanceId, agentInstanceId, StringComparison.Ordinal))
            return false;

        var agent = FindCurrentProcessingAgent();
        if (agent == null || agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return false;

        RunState.turn.selectedAgentDieIndex = 0;
        SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    // Legacy name kept for old drag handler compatibility.
    public bool TryAssignAgent(string agentInstanceId, string situationInstanceId)
    {
        if (string.IsNullOrWhiteSpace(agentInstanceId) || string.IsNullOrWhiteSpace(situationInstanceId))
            return false;
        if (!string.Equals(agentInstanceId, RunState?.turn.processingAgentInstanceId, StringComparison.Ordinal))
            return false;

        if (RunState.turn.selectedAgentDieIndex < 0)
        {
            if (!TrySelectProcessingAgentDie(0))
                return false;
        }

        return TryTestAgainstSituationDie(situationInstanceId, 0);
    }

    public bool TryClearAgentAssignment(string agentInstanceId)
    {
        if (RunState == null || isRunOver)
            return false;
        if (isDuelResolutionPending)
            return false;
        if (RunState.turn.phase != TurnPhase.TargetAndAttack)
            return false;
        if (!string.Equals(agentInstanceId, RunState.turn.processingAgentInstanceId, StringComparison.Ordinal))
            return false;

        RunState.turn.selectedAgentDieIndex = -1;
        SetPhase(TurnPhase.Adjustment);
        return true;
    }

    IEnumerator ResolveDuelAfterPresentation(DuelRollPresentation presentation)
    {
        while (!duelPresentationCompleted)
            yield return null;

        ResolveDuelState(presentation);
        isDuelResolutionPending = false;
        duelPresentationCompleted = false;
    }

    public void NotifyDuelPresentationFinished()
    {
        if (!isDuelResolutionPending)
            return;

        duelPresentationCompleted = true;
    }

    void ResolveDuelState(DuelRollPresentation presentation)
    {
        var agent = FindAgentState(presentation.agentInstanceId);
        if (!IsValidAgentDieIndex(agent, presentation.agentDieIndex))
            return;

        var situation = FindSituationState(presentation.situationInstanceId);
        if (!IsValidSituationDieIndex(situation, presentation.situationDieIndex))
            return;

        agent.remainingDiceFaces.RemoveAt(presentation.agentDieIndex);

        if (presentation.success)
        {
            if (IsValidSituationDieIndex(situation, presentation.situationDieIndex))
                situation.remainingDiceFaces.RemoveAt(presentation.situationDieIndex);
        }
        else if (IsValidSituationDieIndex(situation, presentation.situationDieIndex))
        {
            int currentFace = situation.remainingDiceFaces[presentation.situationDieIndex];
            int reducedFace = currentFace - Math.Max(0, presentation.agentRoll);
            if (reducedFace <= 0)
                situation.remainingDiceFaces.RemoveAt(presentation.situationDieIndex);
            else
                situation.remainingDiceFaces[presentation.situationDieIndex] = reducedFace;
        }

        if (situation.remainingDiceFaces.Count == 0)
            HandleSituationSucceeded(situation.instanceId);

        AdvanceAfterAgentDieSpent(agent.instanceId);
    }

    public int GetUnassignedAgentCount()
    {
        if (RunState == null)
            return 0;

        return GetPendingAgentCount();
    }

    public bool RequestCommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;

        int pendingCount = GetUnassignedAgentCount();
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

        MarkAllPendingAgentsConsumed();
        RunState.turn.processingAgentInstanceId = string.Empty;
        RunState.turn.selectedAgentDieIndex = -1;
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

        var agent = FindCurrentProcessingAgent();
        if (agent == null)
            return false;
        if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return false;

        int pickIndex = RunState.turn.selectedAgentDieIndex;
        if (!IsValidAgentDieIndex(agent, pickIndex))
            pickIndex = 0;

        RunState.turn.selectedAgentDieIndex = pickIndex;
        SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    // Skills are temporarily disabled in the dice-duel migration stage.
    public bool TryUseSkill(
        string skillDefId,
        string selectedAgentInstanceId = null,
        string selectedSituationInstanceId = null,
        int selectedDieIndex = -1)
    {
        return false;
    }

    public bool TryUseSkillBySlotIndex(
        int skillSlotIndex,
        string selectedAgentInstanceId = null,
        string selectedSituationInstanceId = null,
        int selectedDieIndex = -1)
    {
        return false;
    }

    public bool CanUseSkillBySlotIndex(int skillSlotIndex)
    {
        return false;
    }

    public bool SkillRequiresSituationTargetBySlotIndex(int skillSlotIndex)
    {
        return false;
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

                case TurnPhase.AgentRoll:
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
        RunState.ResetTurnTransientState();

        ResetAgentsForNewTurn();

        for (int i = 0; i < RunState.skillCooldowns.Count; i++)
        {
            var skillCooldown = RunState.skillCooldowns[i];
            if (skillCooldown == null)
                continue;

            if (skillCooldown.cooldownRemainingTurns > 0)
                skillCooldown.cooldownRemainingTurns -= 1;
            skillCooldown.usedThisTurn = false;
        }

        SetPhase(TurnPhase.BoardUpdate);
    }

    void ExecuteBoardUpdatePhase()
    {
        RunState.turn.processingAgentInstanceId = string.Empty;
        RunState.turn.selectedAgentDieIndex = -1;

        bool spawned = GameRunBootstrap.TrySpawnPeriodicSituations(RunState, staticData, rng);
        if (spawned)
            StageSpawned?.Invoke(RunState.stage.stageNumber, RunState.stage.activePresetId);

        SetPhase(TurnPhase.AgentRoll);
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
            if (situation.remainingDiceFaces == null || situation.remainingDiceFaces.Count == 0)
                continue;

            situation.deadlineTurnsLeft -= 1;
            if (situation.deadlineTurnsLeft > 0)
                continue;

            if (!situationDefById.TryGetValue(situation.situationDefId, out var situationDef))
            {
                RemoveSituationState(situation.instanceId);
                continue;
            }

            TryApplyEffectBundle(situationDef.failureEffect);

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
        StateChanged?.Invoke();
    }

    bool CanAcceptAgentSelectionInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (isDuelResolutionPending)
            return false;
        if (!string.IsNullOrWhiteSpace(RunState.turn.processingAgentInstanceId))
            return false;

        return RunState.turn.phase == TurnPhase.AgentRoll;
    }

    bool CanAcceptAgentDieSelectionInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (isDuelResolutionPending)
            return false;
        if (RunState.turn.phase != TurnPhase.Adjustment)
            return false;

        var agent = FindCurrentProcessingAgent();
        if (agent == null || agent.actionConsumed)
            return false;

        return agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0;
    }

    bool CanAcceptDuelTargetingInput()
    {
        if (RunState == null || isRunOver)
            return false;
        if (isDuelResolutionPending)
            return false;
        if (RunState.turn.phase != TurnPhase.TargetAndAttack)
            return false;

        var agent = FindCurrentProcessingAgent();
        if (agent == null || agent.actionConsumed)
            return false;

        return IsValidAgentDieIndex(agent, RunState.turn.selectedAgentDieIndex);
    }

    bool CanRequestTurnCommit()
    {
        if (RunState == null || isRunOver)
            return false;
        if (isDuelResolutionPending)
            return false;

        var phase = RunState.turn.phase;
        return phase == TurnPhase.AgentRoll ||
               phase == TurnPhase.Adjustment ||
               phase == TurnPhase.TargetAndAttack;
    }

    public bool CanRollAgent(string agentInstanceId)
    {
        if (!CanAcceptAgentSelectionInput())
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        var agent = FindAgentState(agentInstanceId);
        if (agent == null || agent.actionConsumed)
            return false;

        return agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0;
    }

    // Legacy drag loop is disabled in the new dice-duel flow.
    public bool CanAssignAgent(string agentInstanceId)
    {
        return false;
    }

    public bool TryGetAgentAttackBreakdown(
        string agentInstanceId,
        out int baseAttack,
        out int ruleBonus,
        out int totalAttack)
    {
        baseAttack = 0;
        ruleBonus = 0;
        totalAttack = 0;
        return false;
    }

    public bool IsCurrentProcessingAgent(string agentInstanceId)
    {
        if (RunState == null)
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        return string.Equals(
            RunState.turn.processingAgentInstanceId,
            agentInstanceId,
            StringComparison.Ordinal);
    }

    void AdvanceAfterAgentDieSpent(string actingAgentInstanceId)
    {
        var agent = FindAgentState(actingAgentInstanceId);
        RunState.turn.selectedAgentDieIndex = -1;

        if (agent != null && agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0)
        {
            SetPhase(TurnPhase.Adjustment);
            return;
        }

        if (agent != null)
            agent.actionConsumed = true;

        RunState.turn.processingAgentInstanceId = string.Empty;

        if (GetPendingAgentCount() > 0)
        {
            SetPhase(TurnPhase.AgentRoll);
            return;
        }

        SetPhase(TurnPhase.Settlement);
        AdvanceToDecisionPoint();
    }

    int GetPendingAgentCount()
    {
        if (RunState?.agents == null)
            return 0;

        int count = 0;
        for (int i = 0; i < RunState.agents.Count; i++)
        {
            var agent = RunState.agents[i];
            if (agent == null)
                continue;
            if (agent.actionConsumed)
                continue;
            if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
                continue;

            count += 1;
        }

        return count;
    }

    void MarkAllPendingAgentsConsumed()
    {
        if (RunState?.agents == null)
            return;

        for (int i = 0; i < RunState.agents.Count; i++)
        {
            var agent = RunState.agents[i];
            if (agent == null)
                continue;
            if (agent.actionConsumed)
                continue;
            if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
                continue;

            agent.actionConsumed = true;
        }
    }

    void ResetAgentsForNewTurn()
    {
        if (RunState?.agents == null)
            return;

        for (int i = 0; i < RunState.agents.Count; i++)
        {
            var agent = RunState.agents[i];
            if (agent == null)
                continue;

            if (!agentDefById.TryGetValue(agent.agentDefId, out var agentDef) ||
                agentDef?.diceFaces == null)
            {
                agent.remainingDiceFaces = new List<int>();
                agent.actionConsumed = true;
                continue;
            }

            if (agent.remainingDiceFaces == null)
                agent.remainingDiceFaces = new List<int>(agentDef.diceFaces.Count);
            else
                agent.remainingDiceFaces.Clear();

            for (int dieIndex = 0; dieIndex < agentDef.diceFaces.Count; dieIndex++)
            {
                int face = Math.Max(2, agentDef.diceFaces[dieIndex]);
                agent.remainingDiceFaces.Add(face);
            }

            agent.actionConsumed = agent.remainingDiceFaces.Count == 0;
        }
    }

    void HandleSituationSucceeded(string situationInstanceId)
    {
        var situation = FindSituationState(situationInstanceId);
        if (situation == null)
            return;

        situationDefById.TryGetValue(situation.situationDefId, out var situationDef);

        RemoveSituationState(situation.instanceId);

        if (situationDef == null)
            return;

        TryApplyEffectBundle(situationDef.successReward);
    }

    void HandleSituationFailurePostEffect(SituationState situation, SituationDef situationDef)
    {
        if (situation == null)
            return;

        string mode = NormalizeFailurePersistMode(situationDef?.failurePersistMode);
        if (string.Equals(mode, "resetDeadline", StringComparison.Ordinal))
        {
            int baseDeadline = situationDef != null
                ? Math.Max(1, situationDef.baseDeadlineTurns)
                : 1;
            situation.deadlineTurnsLeft = baseDeadline;
            return;
        }

        RemoveSituationState(situation.instanceId);
    }

    static string NormalizeFailurePersistMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "remove";

        string mode = raw.Trim();
        if (string.Equals(mode, "remove", StringComparison.OrdinalIgnoreCase))
            return "remove";
        if (string.Equals(mode, "resetDeadline", StringComparison.OrdinalIgnoreCase))
            return "resetDeadline";
        return "remove";
    }

    bool TryApplyEffectBundle(EffectBundle effectBundle)
    {
        if (effectBundle?.effects == null)
            return true;

        bool appliedAny = false;
        for (int i = 0; i < effectBundle.effects.Count; i++)
        {
            var effect = effectBundle.effects[i];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                continue;

            string effectType = effect.effectType.Trim();
            int value = ToIntValue(effect.value);

            if (string.Equals(effectType, "stabilityDelta", StringComparison.Ordinal))
            {
                RunState.stability += value;
                appliedAny = true;
                continue;
            }

            if (string.Equals(effectType, "goldDelta", StringComparison.Ordinal))
            {
                RunState.gold += value;
                appliedAny = true;
            }
        }

        if (appliedAny)
            StateChanged?.Invoke();

        return true;
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

    AgentState FindCurrentProcessingAgent()
    {
        if (RunState == null)
            return null;
        if (string.IsNullOrWhiteSpace(RunState.turn.processingAgentInstanceId))
            return null;

        return FindAgentState(RunState.turn.processingAgentInstanceId);
    }

    AgentState FindAgentState(string agentInstanceId)
    {
        for (int i = 0; i < RunState.agents.Count; i++)
        {
            var agent = RunState.agents[i];
            if (agent == null)
                continue;
            if (!string.Equals(agent.instanceId, agentInstanceId, StringComparison.Ordinal))
                continue;

            return agent;
        }

        return null;
    }

    int RollByFace(int face)
    {
        int clampedFace = Math.Max(1, face);
        return rng.Next(1, clampedFace + 1);
    }

    static int ToIntValue(double? value)
    {
        if (!value.HasValue)
            return 0;

        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    static bool IsValidAgentDieIndex(AgentState agent, int dieIndex)
    {
        if (agent?.remainingDiceFaces == null)
            return false;

        return dieIndex >= 0 && dieIndex < agent.remainingDiceFaces.Count;
    }

    static bool IsValidSituationDieIndex(SituationState situation, int dieIndex)
    {
        if (situation?.remainingDiceFaces == null)
            return false;

        return dieIndex >= 0 && dieIndex < situation.remainingDiceFaces.Count;
    }

    void BuildDefinitionLookups(GameStaticDataSet dataSet)
    {
        situationDefById = new Dictionary<string, SituationDef>(StringComparer.Ordinal);
        agentDefById = new Dictionary<string, AgentDef>(StringComparer.Ordinal);

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

        if (dataSet?.agentDefs != null)
        {
            for (int i = 0; i < dataSet.agentDefs.Count; i++)
            {
                var def = dataSet.agentDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.agentId))
                    continue;

                agentDefById[def.agentId] = def;
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
}
