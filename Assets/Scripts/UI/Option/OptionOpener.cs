using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OptionOpener : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private OptionService optionService;
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

        if (optionService == null)
            optionService = GameApp.I?.UI?.Option;

        subscriptions.Add(EventSubscription.Subscribe(button, OnClickOpenOptions));
    }

    void OnDisable()
    {
        subscriptions.Clear();
    }

    private void OnClickOpenOptions()
    {
        if (optionService != null)
            optionService.ToggleOption();
        else
            Debug.LogWarning("[OptionOpener] OptionService 인스턴스가 없습니다!");
    }
}

