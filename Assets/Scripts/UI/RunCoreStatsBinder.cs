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
            Debug.LogWarning("[RunCoreStatsBinder] RunServices is null. Call BeginRun before enabling HUD.");
            return;
        }

        subscriptions.Add(boundRun.PrimaryValue.Subscribe(UpdatePrimaryValue));
        subscriptions.Add(boundRun.SecondaryValue.Subscribe(UpdateSecondaryValue));
        subscriptions.Add(boundRun.Tick.Subscribe(UpdateTick));
        subscriptions.Add(boundRun.UiRevision.Subscribe(_ => UpdateSessionLabel()));
        UpdateSessionLabel();
    }

    void OnDisable()
    {
        subscriptions.Clear();
        boundRun = null;
    }

    void ApplyDefaults()
    {
        SetText(goldText, "Primary: 0");
        SetText(stabilityText, "Secondary: 0");
        SetText(turnText, "Tick: 0");
        SetText(barracksCapacityText, "Session: -");
    }

    void UpdatePrimaryValue(int value)
    {
        SetText(goldText, $"Primary: {value}");
    }

    void UpdateSecondaryValue(int value)
    {
        SetText(stabilityText, $"Secondary: {value}");
    }

    void UpdateTick(int value)
    {
        SetText(turnText, $"Tick: {value}");
    }

    void UpdateSessionLabel()
    {
        string uid = boundRun?.CurrentRunState?.uid;
        if (string.IsNullOrWhiteSpace(uid))
        {
            SetText(barracksCapacityText, "Session: -");
            return;
        }

        string shortUid = uid.Length > 8 ? uid.Substring(0, 8) : uid;
        SetText(barracksCapacityText, $"Session: {shortUid}");
    }

    static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;

        target.text = value;
    }
}

