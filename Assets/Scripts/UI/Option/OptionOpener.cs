using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OptionOpener : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private OptionManager optionManager;
    readonly DisposableBag subscriptions = new();

    void Reset()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        subscriptions.Clear();

        if (button == null)
            button = GetComponent<Button>();
        if (button == null)
            return;

        if (optionManager == null)
            optionManager = GameApp.I?.UI?.Option;

        subscriptions.Add(EventSubscription.Subscribe(button, OnClickOpenOptions));
    }

    void OnDisable()
    {
        subscriptions.Clear();
    }

    private void OnClickOpenOptions()
    {
        if (optionManager != null)
            optionManager.ToggleOption();
        else
            Debug.LogWarning("[OptionOpener] OptionManager 인스턴스가 없습니다!");
    }
}
