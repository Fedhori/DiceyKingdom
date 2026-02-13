using System;
using UnityEngine;

public sealed class DuelManager : MonoBehaviour
{
    GameRunState runState;
    DuelRollPresentation pendingPresentation;

    public static DuelManager Instance { get; private set; }

    public bool IsDuelResolutionPending { get; private set; }

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
        IsDuelResolutionPending = false;
        pendingPresentation = default;
    }

    public bool BeginDuel(
        string agentInstanceId,
        int agentDieIndex,
        int agentDieFace,
        string situationInstanceId,
        int situationDieIndex,
        int situationDieFace)
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (IsDuelResolutionPending)
            return false;

        int resolvedAgentFace = Mathf.Max(1, agentDieFace);
        int resolvedSituationFace = Mathf.Max(1, situationDieFace);
        int agentRoll = RollByFace(resolvedAgentFace);
        int situationRoll = RollByFace(resolvedSituationFace);
        bool success = agentRoll >= situationRoll;

        pendingPresentation = new DuelRollPresentation(
            agentInstanceId,
            agentDieIndex,
            resolvedAgentFace,
            agentRoll,
            situationInstanceId,
            situationDieIndex,
            resolvedSituationFace,
            situationRoll,
            success);
        IsDuelResolutionPending = true;
        DuelRollStarted?.Invoke(pendingPresentation);
        return true;
    }

    public void NotifyDuelPresentationFinished()
    {
        if (!IsDuelResolutionPending)
            return;

        ResolveDuelState(pendingPresentation);
        IsDuelResolutionPending = false;
        pendingPresentation = default;
    }

    void ResolveDuelState(DuelRollPresentation presentation)
    {
        bool removedAgentDie = AgentManager.Instance.RemoveAgentDie(
            presentation.agentInstanceId,
            presentation.agentDieIndex);
        if (!removedAgentDie)
            return;

        if (presentation.success)
        {
            SituationManager.Instance.RemoveSituationDie(
                presentation.situationInstanceId,
                presentation.situationDieIndex);
        }
        else
        {
            SituationManager.Instance.ReduceSituationDie(
                presentation.situationInstanceId,
                presentation.situationDieIndex,
                presentation.agentRoll);
        }

        if (SituationManager.Instance.GetRemainingDieCount(presentation.situationInstanceId) == 0)
            SituationManager.Instance.HandleSituationSucceeded(presentation.situationInstanceId);

        AgentManager.Instance.AdvanceAfterAgentDieSpent(presentation.agentInstanceId);
    }

    int RollByFace(int face)
    {
        int clampedFace = Mathf.Max(1, face);
        return GameManager.Instance.Rng.Next(1, clampedFace + 1);
    }
}
