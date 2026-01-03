using Data;
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

    public Sprite GetIconSprite()
    {
        return iconImage != null ? iconImage.sprite : null;
    }

    public void SetIconVisible(bool visible)
    {
        if (iconImage == null)
            return;

        if (!visible)
        {
            iconImage.enabled = false;
            return;
        }

        iconImage.enabled = iconImage.sprite != null;
    }

    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    public void SetRarity(ItemRarity rarity)
    {
        if (backgroundImage != null)
            backgroundImage.color = Colors.GetRarityColor(rarity);
    }

    public void SetHighlight(bool active)
    {
        if (highlightMask != null)
            highlightMask.SetActive(active);
    }

    public void SetSelected(bool selected)
    {
        SetHighlight(selected);
    }
}
