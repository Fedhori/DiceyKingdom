using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public sealed class TooltipView : MonoBehaviour
{
    // [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Transform keywordRoot;
    [SerializeField] TooltipKeywordRow keywordRowPrefab;
    [SerializeField] Image rarityPanelImage;
    [SerializeField] TMP_Text rarityText;
    [SerializeField] GameObject toggleButtonRoot;
    [SerializeField] Button toggleButton;
    [SerializeField] TMP_Text toggleButtonText;

    public RectTransform rectTransform;
    readonly List<TooltipKeywordRow> keywordRows = new();

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
            rectTransform.pivot = new Vector2(0f, 1f);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(HandleToggleButtonClicked);

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

        BuildKeywordRows(model.KeywordEntries);
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
        SetToggleButton(false, false);
        ClearKeywordRows();
        gameObject.SetActive(false);
    }

    public void SetToggleButton(bool visible, bool showingUpgrade)
    {
        var root = toggleButtonRoot != null
            ? toggleButtonRoot
            : (toggleButton != null ? toggleButton.gameObject : null);

        if (root != null)
            root.SetActive(visible);

        if (!visible || toggleButtonText == null)
            return;

        string key = showingUpgrade ? "tooltip.item.view" : "tooltip.upgrade.view";
        var loc = new LocalizedString("tooltip", key);
        toggleButtonText.text = loc.GetLocalizedString();
    }

    void HandleToggleButtonClicked()
    {
        TooltipManager.Instance?.TogglePinnedView();
    }

    void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(HandleToggleButtonClicked);
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

    void BuildKeywordRows(IReadOnlyList<TooltipKeywordEntry> entries)
    {
        if (keywordRoot == null || keywordRowPrefab == null)
            return;

        ClearKeywordRows();

        if (entries == null || entries.Count == 0)
        {
            keywordRoot.gameObject.SetActive(false);
            return;
        }

        keywordRoot.gameObject.SetActive(true);

        for (int i = 0; i < entries.Count; i++)
        {
            var row = Instantiate(keywordRowPrefab, keywordRoot);
            row.Bind(entries[i]);
            keywordRows.Add(row);
        }
    }

    void ClearKeywordRows()
    {
        if (keywordRows.Count == 0)
        {
            if (keywordRoot != null)
                keywordRoot.gameObject.SetActive(false);
            return;
        }

        for (int i = keywordRows.Count - 1; i >= 0; i--)
        {
            var row = keywordRows[i];
            if (row != null)
                Destroy(row.gameObject);
        }
        keywordRows.Clear();

        if (keywordRoot != null)
            keywordRoot.gameObject.SetActive(false);
    }
}
