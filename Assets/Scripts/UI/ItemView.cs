using UnityEngine;
using UnityEngine.UI;

public sealed class ItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject highlightMask;

    public void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    public void SetIconVisible(bool visible)
    {
        if (iconImage != null)
            iconImage.enabled = visible;
    }

    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    public void SetHighlight(bool active)
    {
        if (highlightMask != null)
            highlightMask.SetActive(active);
    }
}
