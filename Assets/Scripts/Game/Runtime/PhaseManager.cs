using System;
using UnityEngine;

public sealed class PhaseManager : MonoBehaviour
{
    TurnPhase currentPhase = TurnPhase.TurnStart;
    int turnNumber = 1;
    GameRunState runState;

    public static PhaseManager Instance { get; private set; }

    public event Action<TurnPhase> PhaseChanged;
    public event Action<int> TurnNumberChanged;

    public TurnPhase CurrentPhase
    {
        get => currentPhase;
        private set
        {
            if (currentPhase == value)
                return;

            currentPhase = value;
            if (runState != null)
                runState.turn.phase = value;
            PhaseChanged?.Invoke(value);
        }
    }

    public int TurnNumber
    {
        get => turnNumber;
        private set
        {
            if (turnNumber == value)
                return;

            turnNumber = value;
            if (runState != null)
                runState.turn.turnNumber = value;
            TurnNumberChanged?.Invoke(value);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeForRun(GameRunState state)
    {
        runState = state;
        TurnNumber = state != null ? state.turn.turnNumber : 1;
        CurrentPhase = state != null ? state.turn.phase : TurnPhase.TurnStart;
    }

    public void SetPhase(TurnPhase phase)
    {
        CurrentPhase = phase;
    }

    public bool CommitAdjustmentPhase()
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (DuelManager.Instance.IsDuelResolutionPending)
            return false;
        if (CurrentPhase != TurnPhase.Adjustment)
            return false;

        var agent = AgentManager.Instance.FindCurrentProcessingAgent();
        if (agent == null)
            return false;
        if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return false;

        int pickIndex = runState.turn.selectedAgentDieIndex;
        if (!AgentManager.IsValidAgentDieIndex(agent, pickIndex))
            pickIndex = 0;

        runState.turn.selectedAgentDieIndex = pickIndex;
        SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    public bool CommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;
        if (AgentManager.Instance.GetPendingAgentCount() > 0)
            return false;

        runState.turn.processingAgentInstanceId = string.Empty;
        runState.turn.selectedAgentDieIndex = -1;
        SetPhase(TurnPhase.Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public int RequestCommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return -1;

        int pendingCount = AgentManager.Instance.GetPendingAgentCount();
        if (pendingCount > 0)
            return pendingCount;

        CommitAssignmentPhase();
        return 0;
    }

    public bool ConfirmCommitAssignmentPhase()
    {
        if (!CanRequestTurnCommit())
            return false;

        AgentManager.Instance.MarkAllPendingAgentsConsumed();
        runState.turn.processingAgentInstanceId = string.Empty;
        runState.turn.selectedAgentDieIndex = -1;
        SetPhase(TurnPhase.Settlement);
        AdvanceToDecisionPoint();
        return true;
    }

    public bool CanRequestTurnCommit()
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (DuelManager.Instance.IsDuelResolutionPending)
            return false;

        return CurrentPhase == TurnPhase.AgentRoll ||
               CurrentPhase == TurnPhase.Adjustment ||
               CurrentPhase == TurnPhase.TargetAndAttack;
    }

    public void AdvanceToDecisionPoint()
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return;

        bool keepRunning = true;
        while (keepRunning && !GameManager.Instance.IsRunOver)
        {
            switch (CurrentPhase)
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
        runState.ResetTurnTransientState();
        AgentManager.Instance.ResetAgentsForNewTurn();

        for (int i = 0; i < runState.skillCooldowns.Count; i++)
        {
            var skillCooldown = runState.skillCooldowns[i];
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
        runState.turn.processingAgentInstanceId = string.Empty;
        runState.turn.selectedAgentDieIndex = -1;
        SituationManager.Instance.TrySpawnPeriodicSituations();
        SetPhase(TurnPhase.AgentRoll);
    }

    void ExecuteSettlementPhase()
    {
        SituationManager.Instance.ProcessSettlement();
        SetPhase(TurnPhase.EndTurn);
    }

    void ExecuteEndTurnPhase()
    {
        PlayerManager.Instance.ClampResources();

        if (GameManager.Instance.EvaluateGameOver())
            return;

        TurnNumber += 1;
        SetPhase(TurnPhase.TurnStart);
    }
}
