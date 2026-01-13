using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TooltipView : MonoBehaviour
{
    // [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image rarityPanelImage;
    [SerializeField] TMP_Text rarityText;

    public RectTransform rectTransform;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
            rectTransform.pivot = new Vector2(0f, 1f);

        gameObject.SetActive(false);
    }

    public void Show(TooltipModel model)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = model.Title ?? string.Empty;

        if (descriptionText != null)
            descriptionText.text = model.Body ?? string.Empty;

        ApplyRarity(model.Rarity, model.RarityLabelOverride, model.Kind);

        // if (iconImage != null)
        // {
        //     if (model.Icon != null)
        //     {
        //         iconImage.sprite = model.Icon;
        //         iconImage.enabled = true;
        //     }
        //     else
        //     {
        //         iconImage.sprite = null;
        //         iconImage.enabled = false;
        //     }
        // }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void ApplyRarity(ItemRarity rarity, string labelOverride, TooltipKind kind)
    {
        if (rarityPanelImage != null)
            rarityPanelImage.color = kind == TooltipKind.Upgrade ? Colors.Upgrade : GetRarityColor(rarity);

        if (rarityText != null)
            rarityText.text = string.IsNullOrEmpty(labelOverride) ? GetRarityLabel(rarity) : labelOverride;
    }

    Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Colors.Common;
            case ItemRarity.Uncommon:
                return Colors.Uncommon;
            case ItemRarity.Rare:
                return Colors.Rare;
            case ItemRarity.Legendary:
                return Colors.Legendary;
            default:
                return Colors.Common;
        }
    }

    string GetRarityLabel(ItemRarity rarity)
    {
        string key = rarity switch
        {
            ItemRarity.Common => "tooltip.normal.label",
            ItemRarity.Uncommon => "tooltip.special.label",
            ItemRarity.Rare => "tooltip.rare.label",
            ItemRarity.Legendary => "tooltip.legendary.label",
            _ => "tooltip.normal.label"
        };

        var loc = new UnityEngine.Localization.LocalizedString("tooltip", key);
        return loc.GetLocalizedString();
    }
}
