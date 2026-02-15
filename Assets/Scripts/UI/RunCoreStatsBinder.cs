using TMPro;
using UnityEngine;

/// <summary>
/// Subscribes to RunServices observables and writes core run metrics to TMP text fields.
/// </summary>
public sealed class RunCoreStatsBinder : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text stabilityText;
    [SerializeField] TMP_Text turnText;
    [SerializeField] TMP_Text barracksCapacityText;
    [SerializeField] TMP_Text candidatesCountText;
    [SerializeField] TMP_Text adventurersCountText;
    [SerializeField] TMP_Text missionsCountText;

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
        subscriptions.Add(boundRun.CandidatesCount.Subscribe(UpdateCandidatesCount));
        subscriptions.Add(boundRun.AdventurersCount.Subscribe(UpdateAdventurersCount));
        subscriptions.Add(boundRun.MissionsCount.Subscribe(UpdateMissionsCount));
    }

    void OnDisable()
    {
        subscriptions.Clear();
        boundRun = null;
    }

    void ApplyDefaults()
    {
        SetText(goldText, "0");
        SetText(stabilityText, "0/0");
        SetText(turnText, "0");
        SetText(barracksCapacityText, "0");
        SetText(candidatesCountText, "0");
        SetText(adventurersCountText, "0");
        SetText(missionsCountText, "0");
    }

    void UpdateGold(int value)
    {
        SetText(goldText, value.ToString());
    }

    void UpdateStability(int _)
    {
        if (boundRun == null)
        {
            SetText(stabilityText, "0/0");
            return;
        }

        SetText(stabilityText, $"{boundRun.Stability.Value}/{boundRun.StabilityMax.Value}");
    }

    void UpdateTurn(int value)
    {
        SetText(turnText, value.ToString());
    }

    void UpdateBarracksCapacity(int value)
    {
        SetText(barracksCapacityText, value.ToString());
    }

    void UpdateCandidatesCount(int value)
    {
        SetText(candidatesCountText, value.ToString());
    }

    void UpdateAdventurersCount(int value)
    {
        SetText(adventurersCountText, value.ToString());
    }

    void UpdateMissionsCount(int value)
    {
        SetText(missionsCountText, value.ToString());
    }

    static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;

        target.text = value;
    }
}

