using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    [SerializeField] private GameObject rewardOverlay;
    [SerializeField] private LocalizeStringEvent earnedCurrencyText;

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
    public void Open()
    {
        isOpen = true;
        
        CurrencyManager.Instance?.AddCurrency(GameConfig.BaseIncome);
        PlayerManager.Instance.Current.BallCount += GameConfig.BaseBallIncome;
        
        UpdateEarnedCurrency();
        
        if(rewardOverlay != null)
            rewardOverlay.SetActive(true);

        // 일단은 바로 닫히게 해둠. 나중에 대응해야 할듯?
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        rewardOverlay.SetActive(false);
        isOpen = false;

        FlowManager.Instance?.OnRewardClosed();
    }

    private void UpdateEarnedCurrency()
    {
        if (earnedCurrencyText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = GameConfig.BaseIncome.ToString(CultureInfo.InvariantCulture);
    }
}
