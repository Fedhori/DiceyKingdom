using System;
using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class TooltipView : MonoBehaviour
{
    [FormerlySerializedAs("rarityText")]
    [SerializeField] TMP_Text typeText;
    
    [SerializeField] Transform keywordRoot;
    [SerializeField] TooltipKeywordRow keywordRowPrefab;
    
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image nameImage;
    [SerializeField] Image typeImage;
    
    [SerializeField] TMP_Text descriptionText;
    
    [SerializeField] GameObject toggleButtonRoot;
    [SerializeField] Button toggleButton;
    [SerializeField] TMP_Text toggleButtonText;
    [SerializeField] Image toggleButtonImage;

    public RectTransform rectTransform;
    readonly List<TooltipKeywordRow> keywordRows = new();
    Color nameImageDefaultColor;
    bool hasNameImageDefaultColor;
    Action toggleButtonAction;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
            rectTransform.pivot = new Vector2(0f, 1f);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(HandleToggleButtonClicked);

        if (nameImage != null)
        {
            nameImageDefaultColor = nameImage.color;
            hasNameImageDefaultColor = true;
        }

        gameObject.SetActive(false);
    }

    public void Show(TooltipModel model)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = model.title ?? string.Empty;

        if (descriptionText != null)
            descriptionText.text = model.body ?? string.Empty;

        BuildKeywordRows(model.keywordEntries);
        ApplyType(model.kind);
        ApplyNameBackground(model.kind, model.rarity);

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
        SetToggleButton(false, null, default, false, null);
        ClearKeywordRows();
        gameObject.SetActive(false);
    }

    public void SetToggleButton(bool visible, string labelKey, Color backgroundColor, bool interactable, Action onClick)
    {
        var root = toggleButtonRoot != null
            ? toggleButtonRoot
            : (toggleButton != null ? toggleButton.gameObject : null);

        if (root != null)
            root.SetActive(visible);

        toggleButtonAction = onClick;

        if (toggleButton != null)
            toggleButton.interactable = visible && interactable;

        if (toggleButtonImage == null && toggleButton != null)
            toggleButtonImage = toggleButton.targetGraphic as Image;

        if (visible && toggleButtonImage != null)
            toggleButtonImage.color = backgroundColor;

        if (!visible || toggleButtonText == null)
            return;

        if (string.IsNullOrEmpty(labelKey))
        {
            toggleButtonText.text = string.Empty;
            return;
        }

        var loc = new LocalizedString("tooltip", labelKey);
        toggleButtonText.text = loc.GetLocalizedString();
    }

    void HandleToggleButtonClicked()
    {
        toggleButtonAction?.Invoke();
    }

    void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(HandleToggleButtonClicked);
    }

    void ApplyType(TooltipKind kind)
    {
        if (kind == TooltipKind.Simple)
        {
            if (typeImage != null)
                typeImage.gameObject.SetActive(false);
            if (typeText != null)
            {
                typeText.gameObject.SetActive(false);
                typeText.text = string.Empty;
            }
            return;
        }

        var type = ResolveType(kind);

        if (typeImage != null)
        {
            typeImage.gameObject.SetActive(true);
            typeImage.color = GetTypeColor(type);
        }

        if (typeText != null)
        {
            typeText.gameObject.SetActive(true);
            typeText.text = GetTypeLabel(type);
        }
    }

    void ApplyNameBackground(TooltipKind kind, ItemRarity rarity)
    {
        if (nameImage == null)
            return;

        if (kind == TooltipKind.Simple)
        {
            if (hasNameImageDefaultColor)
                nameImage.color = nameImageDefaultColor;
            return;
        }

        nameImage.color = Colors.GetRarityColor(rarity);
    }

    static ProductType ResolveType(TooltipKind kind)
    {
        return kind == TooltipKind.Upgrade ? ProductType.Upgrade : ProductType.Item;
    }

    Color GetTypeColor(ProductType type)
    {
        return type == ProductType.Upgrade ? Colors.Upgrade : Colors.Item;
    }

    string GetTypeLabel(ProductType type)
    {
        string key = type switch
        {
            ProductType.Item => "tooltip.item.label",
            ProductType.Upgrade => "tooltip.upgrade.label",
            _ => "tooltip.item.label"
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
