using System;
using UnityEngine;

public sealed class GameTurnOrchestrator : MonoBehaviour
{
    [SerializeField] bool autoStartOnAwake = true;
    [SerializeField] bool useFixedSeed;
    [SerializeField] int fixedSeed = 1001;

    GameStaticDataSet staticData;
    System.Random rng;
    bool isRunOver;

    public GameRunState RunState { get; private set; }

    public event Action<GameRunState> RunStarted;
    public event Action<TurnPhase> PhaseChanged;
    public event Action<GameRunState> RunEnded;
    public event Action<int, string> StageSpawned;

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
        if (!TryMoveToPhase(TurnPhase.P2Assignment, TurnPhase.P3Roll))
            return false;

        return true;
    }

    public bool CommitRollPhase()
    {
        if (!TryMoveToPhase(TurnPhase.P3Roll, TurnPhase.P4Adjustment))
            return false;

        return true;
    }

    public bool CommitAdjustmentPhase()
    {
        if (!TryMoveToPhase(TurnPhase.P4Adjustment, TurnPhase.P5Settlement))
            return false;

        // P5, P6, 다음 턴 P0/P1까지 자동 처리한 뒤 P2에서 대기.
        AdvanceToDecisionPoint();
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
        bool spawned = GameRunBootstrap.TrySpawnStagePresetIfBoardCleared(RunState, staticData, rng);
        if (spawned)
            StageSpawned?.Invoke(RunState.stage.stageNumber, RunState.stage.activePresetId);

        SetPhase(TurnPhase.P2Assignment);
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
}
