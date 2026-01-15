using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }
    [SerializeField] private GameObject resultOverlay;
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

        int startCurrency = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentCurrency : 0;

        int income = GameConfig.BaseIncome;
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            income += player.BaseIncomeBonus;
        CurrencyManager.Instance?.AddCurrency(income);

        int endCurrency = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentCurrency : startCurrency;
        UpdateEarnedCurrency(Mathf.Max(0, endCurrency - startCurrency));
        
        if(resultOverlay != null)
            resultOverlay.SetActive(true);

        // 일단은 바로 닫히게 해둠. 나중에 대응해야 할듯?
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        resultOverlay.SetActive(false);
        isOpen = false;

        StageManager.Instance?.OnResultClosed();
    }

    private void UpdateEarnedCurrency(int earned)
    {
        if (earnedCurrencyText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = earned.ToString(CultureInfo.InvariantCulture);
    }
}
