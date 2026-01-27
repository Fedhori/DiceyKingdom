using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Components;

public sealed class ResultManager : MonoBehaviour
{
    public static ResultManager Instance { get; private set; }
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private LocalizeStringEvent earnedCurrencyText;
    [SerializeField] private ResultOverlayView resultOverlayView;
    [SerializeField] private SlidePanelLean resultPanelSlide;

    readonly List<DamageTrackingManager.ItemDamageSnapshot> damageRecords = new();
    bool isOpen;
    int lastEarnedIncome;
    IncomeBreakdown lastIncomeBreakdown;

    public readonly struct IncomeBreakdown
    {
        public int BaseIncome { get; }
        public int InterestIncome { get; }
        public int TotalIncome { get; }
        public int InterestStep { get; }
        public int InterestCap { get; }

        public IncomeBreakdown(int baseIncome, int interestIncome, int totalIncome, int interestStep, int interestCap)
        {
            BaseIncome = baseIncome;
            InterestIncome = interestIncome;
            TotalIncome = totalIncome;
            InterestStep = interestStep;
            InterestCap = interestCap;
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

    // 스테이지당 보상으로 바뀔거니까, 여기 말고 스테이지 끝날 때 띄우게
    public void Open()
    {
        OpenInternal(false, lastEarnedIncome, lastIncomeBreakdown);
    }

    public void OpenWithIncome(IncomeBreakdown breakdown)
    {
        lastIncomeBreakdown = breakdown;
        lastEarnedIncome = Mathf.Max(0, breakdown.TotalIncome);
        OpenInternal(true, lastEarnedIncome, lastIncomeBreakdown);
    }

    public void Reopen()
    {
        Open();
    }

    void OpenInternal(bool grantIncome, int income, IncomeBreakdown breakdown)
    {
        isOpen = true;

        if (grantIncome && income > 0)
            CurrencyManager.Instance?.AddCurrency(income);

        UpdateEarnedCurrency(Mathf.Max(0, income), breakdown);
        RebuildDamageRecords();
        
        if(resultOverlay != null)
            resultOverlay.SetActive(true);

        if (resultOverlayView != null)
            resultOverlayView.BuildRows(damageRecords, GetMaxDamage());

        resultPanelSlide?.Show();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (resultPanelSlide != null)
        {
            resultPanelSlide.Hide(() =>
            {
                if (resultOverlay != null)
                    resultOverlay.SetActive(false);

                isOpen = false;
            });
            return;
        }

        if (resultOverlay != null)
            resultOverlay.SetActive(false);
    }

    void UpdateEarnedCurrency(int earned, IncomeBreakdown breakdown)
    {
        if (earnedCurrencyText == null)
            return;

        var dict = new Dictionary<string, object>
        {
            ["totalIncome"] = $"${earned.ToString(CultureInfo.InvariantCulture)}",
            ["baseIncome"] = $"${Mathf.Max(0, breakdown.BaseIncome).ToString(CultureInfo.InvariantCulture)}",
            ["interestIncome"] = $"${Mathf.Max(0, breakdown.InterestIncome).ToString(CultureInfo.InvariantCulture)}",
            ["interestStep"] = $"${Mathf.Max(1, breakdown.InterestStep).ToString(CultureInfo.InvariantCulture)}",
            ["interestCap"] = $"${Mathf.Max(0, breakdown.InterestCap).ToString(CultureInfo.InvariantCulture)}"
        };

        earnedCurrencyText.StringReference.Arguments = new object[] { dict };
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
        _ = GetMaxDamage();
    }

    double GetMaxDamage()
    {
        if (damageRecords.Count == 0)
            return 0d;

        return damageRecords[0].Damage;
    }
}
