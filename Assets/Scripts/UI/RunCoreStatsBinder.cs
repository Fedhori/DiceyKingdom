using TMPro;
using UnityEngine;




public sealed class RunCoreStatsBinder : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text stabilityText;
    [SerializeField] TMP_Text turnText;
    [SerializeField] TMP_Text barracksCapacityText;

    readonly DisposableBag subscriptions = new();
    RunServices boundRun;

    void OnEnable()
    {
        subscriptions.Clear();
        ApplyDefaults();

        boundRun = GameApp.I?.Run;
        if (boundRun == null)
        {
            Debug.LogError("[RunCoreStatsBinder] RunServices is null. Ensure this UI is enabled after BeginRun.");
            return;
        }

        subscriptions.Add(boundRun.Gold.Subscribe(UpdateGold));
        subscriptions.Add(boundRun.Stability.Subscribe(UpdateStability));
        subscriptions.Add(boundRun.StabilityMax.Subscribe(UpdateStability));
        subscriptions.Add(boundRun.Turn.Subscribe(UpdateTurn));
        subscriptions.Add(boundRun.BarracksCapacity.Subscribe(UpdateBarracksCapacity));
    }

    void OnDisable()
    {
        subscriptions.Clear();
        boundRun = null;
    }

    void ApplyDefaults()
    {
        SetText(goldText, "Gold: 0");
        SetText(stabilityText, "Stability: 0/0");
        SetText(turnText, "Turn: 0");
        SetText(barracksCapacityText, "Capacity: 0");
    }

    void UpdateGold(int value)
    {
        SetText(goldText, $"Gold: {value}");
    }

    void UpdateStability(int _)
    {
        if (boundRun == null)
        {
            SetText(stabilityText, "Stability: 0/0");
            return;
        }

        SetText(stabilityText, $"Stability: {boundRun.Stability.Value}/{boundRun.StabilityMax.Value}");
    }

    void UpdateTurn(int value)
    {
        SetText(turnText, $"Turn: {value}");
    }

    void UpdateBarracksCapacity(int value)
    {
        SetText(barracksCapacityText, $"Capacity: {value}");
    }

    static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;

        target.text = value;
    }
}

