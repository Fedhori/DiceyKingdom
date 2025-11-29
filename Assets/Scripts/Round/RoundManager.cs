// RoundManager.cs
using UnityEngine;

public sealed class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    StageInstance currentStage;
    int currentRoundIndex;
    bool roundActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartRound(StageInstance stage, int roundIndex)
    {
        currentStage = stage;
        currentRoundIndex = roundIndex;
        roundActive = true;

        // TODO: 여기서 라운드 시작 시 필요한 초기화 수행
        // 예: 볼 스폰, 필드 리셋 등.
    }

    /// <summary>
    /// "구슬이 전부 바닥에 떨어진 시점"에서 네가 직접 호출해줄 API.
    /// </summary>
    public void NotifyAllBallsDestroyed()
    {
        if (!roundActive)
            return;

        roundActive = false;
        Debug.Log("[RoundManager] All balls destroyed. Round finished.");

        StageManager.Instance?.HandleRoundFinished();
    }
}