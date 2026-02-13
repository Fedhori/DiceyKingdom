using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OptionOpener : MonoBehaviour
{
    [SerializeField] private Button button;

    void Reset()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClickOpenOptions);
        button.onClick.AddListener(OnClickOpenOptions);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClickOpenOptions);
    }

    private void OnClickOpenOptions()
    {
        if (OptionManager.Instance != null)
            OptionManager.Instance.ToggleOption();
        else
            Debug.LogWarning("[OptionOpener] OptionManager 인스턴스가 없습니다!");
    }
}
