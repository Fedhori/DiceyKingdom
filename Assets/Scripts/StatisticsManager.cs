using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }
    [SerializeField] private GameObject rewardOverlay;
    [SerializeField] private LocalizeStringEvent earnedScoreText;
    [SerializeField] private LocalizeStringEvent earnedCurrencyText;
    [SerializeField] private LocalizeStringEvent earnedBallsText;

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

    // 스테이지당 보상으로 바뀔거니까, 여기 말고 스테이지 끝날 때 띄우게
    public void Open(bool isStageClear)
    {
        isOpen = true;
        
        CurrencyManager.Instance?.AddCurrency(GameConfig.BaseRoundIncome);
        PlayerManager.Instance.Current.BallCount += GameConfig.BaseBallIncome;

        UpdateEarnedScore();
        UpdateEarnedCurrency();
        UpdateEarnedBalls(isStageClear);
        
        if(rewardOverlay != null)
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

   private void UpdateEarnedScore()
    {
        if (earnedScoreText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = (ScoreManager.Instance.TotalScore - ScoreManager.Instance.previousScore).ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateEarnedCurrency()
    {
        if (earnedCurrencyText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = GameConfig.BaseRoundIncome.ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateEarnedBalls(bool isShow)
    {
        if (earnedBallsText == null)
            return;
        
        if (!isShow)
        {
            earnedBallsText.gameObject.SetActive(false);
            return;
        }
        
        earnedBallsText.gameObject.SetActive(true);
        if (earnedBallsText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = GameConfig.BaseBallIncome.ToString(CultureInfo.InvariantCulture);
    }
}
