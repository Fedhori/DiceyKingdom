using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private LocalizeStringEvent earnedCurrencyText;

    readonly List<DamageTrackingManager.ItemDamageSnapshot> damageRecords = new();
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

        int income = GameConfig.BaseIncome;
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            income += player.BaseIncomeBonus;
        CurrencyManager.Instance?.AddCurrency(income);

        UpdateEarnedCurrency(Mathf.Max(0, income));
        RebuildDamageRecords();
        
        if(resultOverlay != null)
            resultOverlay.SetActive(true);

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

    void RebuildDamageRecords()
    {
        damageRecords.Clear();

        var tracker = DamageTrackingManager.Instance;
        if (tracker == null)
            return;

        var records = tracker.GetItemDamageRecords();
        if (records == null || records.Count == 0)
            return;

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (record.Item == null || record.Damage <= 0d)
                continue;

            damageRecords.Add(record);
        }

        damageRecords.Sort((a, b) => b.Damage.CompareTo(a.Damage));
    }
}
