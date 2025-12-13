using UnityEngine;

public sealed class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    [SerializeField] private GameObject rewardOverlay;

    bool isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Open(bool isStageClear)
    {
        isOpen = true;
        
        if (isStageClear)
            PlayerManager.Instance.Current.BallDeck.Add(GameConfig.BasicBallId, 3);
        CurrencyManager.Instance?.AddCurrency(GameConfig.BaseRoundIncome);

        rewardOverlay.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen)
            return;

        rewardOverlay.SetActive(false);
        isOpen = false;

        FlowManager.Instance?.OnRewardClosed();
    }
}