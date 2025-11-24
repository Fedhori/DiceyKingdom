using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIGaugeBar : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI valueLabel;

    [Header("Options")]
    [SerializeField] private bool showLabel = true;
    [SerializeField] private string labelFormat = "{0:N0} / {1:N0}";

    private float _current;
    private float _max = 1f;

    private void Awake()
    {
        EnsureFillImageMode();
        Refresh();
    }

    private void OnDisable()
    {
        if (valueLabel != null)
        {
            valueLabel.gameObject.SetActive(false);
        }
    }

    public void UpdateFill(float newCurrent, float newMax)
    {
        if (Mathf.Approximately(_current, newCurrent) && Mathf.Approximately(_max, newMax))
        {
            return;
        }

        _max = Mathf.Max(0.0001f, newMax);
        _current = Mathf.Clamp(newCurrent, 0f, _max);

        Refresh();
    }

    public void SetLabelVisible(bool visible)
    {
        showLabel = visible;
        if (valueLabel != null)
        {
            valueLabel.gameObject.SetActive(showLabel);
        }
    }

    private void Refresh()
    {
        var ratio = Mathf.Clamp01(_current / Mathf.Max(0.0001f, _max));

        if (fillImage != null)
        {
            fillImage.fillAmount = ratio;
        }

        if (valueLabel != null)
        {
            valueLabel.gameObject.SetActive(showLabel);
            if (showLabel)
            {
                valueLabel.text = string.Format(labelFormat, _current, _max);
            }
        }
    }

    private void EnsureFillImageMode()
    {
        if (fillImage == null)
        {
            return;
        }

        if (fillImage.type != Image.Type.Filled)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
