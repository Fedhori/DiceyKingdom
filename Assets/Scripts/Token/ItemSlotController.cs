using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Data;

[RequireComponent(typeof(RectTransform))]
public class ItemSlotController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    public int SlotIndex { get; private set; } = -1;
    public ItemInstance Instance { get; private set; }
    ItemInstance previewInstance;
    bool hasPreview;
    bool isSelected;
    bool isHighlighted;

    [SerializeField] RectTransform rectTransform;
    public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
    [SerializeField] RectTransform dropArea;
    public RectTransform DropArea => dropArea != null ? dropArea : RectTransform;
    [SerializeField] ItemView itemView;
    [SerializeField] ItemTooltipTarget tooltipTarget;
    [SerializeField] Transform upgradeIconContainer;
    [SerializeField] ItemView upgradeIconPrefab;
    [SerializeField] GameObject upgradeVfxRoot;
    readonly List<ItemView> upgradeIcons = new();

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (StageManager.Instance.CurrentPhase == StagePhase.Shop)
        {
            var upgradeInventory = UpgradeInventoryManager.Instance;
            if (upgradeInventory != null && upgradeInventory.HasSelection)
            {
                upgradeInventory.TryApplySelectedUpgradeAt(SlotIndex);
                return;
            }

            if (ShopManager.Instance.IsUpgradeSelectionActive)
            {
                ShopManager.Instance.TryApplySelectedUpgradeAt(SlotIndex);
                return;
            }
        }

        if (Instance != null)
        {
            ItemSlotManager.Instance?.ToggleSlotSelection(this);
            return;
        }

        if (StageManager.Instance.CurrentPhase != StagePhase.Shop)
            return;

        ShopManager.Instance.TryPurchaseSelectedItemAt(SlotIndex);
    }

    public void SetSlotIndex(int index)
    {
        SlotIndex = index;
    }

    public void Bind(ItemInstance instance)
    {
        if (!ReferenceEquals(Instance, instance))
        {
            if (Instance != null)
            {
                Instance.OnUpgradeChanged -= HandleUpgradeChanged;
                Instance.OnUpgradesChanged -= HandleUpgradeChanged;
            }

            Instance = instance;

            if (Instance != null)
            {
                Instance.OnUpgradeChanged += HandleUpgradeChanged;
                Instance.OnUpgradesChanged += HandleUpgradeChanged;
            }
        }

        UpdateView();
    }

    public void SetPreview(ItemInstance preview)
    {
        hasPreview = true;
        previewInstance = preview;
        UpdateView();
    }

    public void ClearPreview()
    {
        if (!hasPreview)
            return;

        hasPreview = false;
        previewInstance = null;
        UpdateView();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Instance == null)
            return;

        ItemSlotManager.Instance?.BeginDrag(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Instance == null)
            return;

        ItemSlotManager.Instance?.EndDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ItemSlotManager.Instance?.UpdateDrag(this, eventData.position);
    }

    void UpdateView()
    {
        var displayInstance = hasPreview ? previewInstance : Instance;

        if (itemView != null)
        {
            itemView.SetIcon(SpriteCache.GetItemSprite(displayInstance?.Id));

            if (displayInstance != null && ItemRepository.TryGet(displayInstance.Id, out var dto) && dto != null)
                itemView.SetRarity(dto.rarity);
            else
                itemView.SetRarity(ItemRarity.Common);
        }

        if (tooltipTarget != null)
        {
            if (displayInstance != null)
                tooltipTarget.Bind(displayInstance);
            else
                tooltipTarget.Clear();
        }

        if (upgradeVfxRoot != null)
        {
            bool hasUpgrade = displayInstance != null && displayInstance.Upgrades.Count > 0;
            upgradeVfxRoot.SetActive(hasUpgrade);
        }

        RefreshUpgradeIcons(displayInstance);
    }

    void HandleUpgradeChanged(ItemInstance instance)
    {
        if (!ReferenceEquals(Instance, instance))
            return;

        UpdateView();
    }

    void RefreshUpgradeIcons(ItemInstance displayInstance)
    {
        if (upgradeIconContainer == null || upgradeIconPrefab == null)
            return;

        var upgrades = displayInstance != null ? displayInstance.Upgrades : null;
        int required = upgrades != null ? upgrades.Count : 0;

        EnsureUpgradeIconCount(required);

        for (int i = 0; i < upgradeIcons.Count; i++)
        {
            var icon = upgradeIcons[i];
            if (icon == null)
                continue;

            bool active = i < required;
            icon.gameObject.SetActive(active);
            if (!active)
                continue;

            var upgrade = upgrades[i];
            icon.SetIcon(SpriteCache.GetUpgradeSprite(upgrade?.Id));
            icon.SetRarity(upgrade != null ? upgrade.Rarity : ItemRarity.Common);
        }
    }

    void EnsureUpgradeIconCount(int required)
    {
        for (int i = upgradeIcons.Count - 1; i >= required; i--)
        {
            var icon = upgradeIcons[i];
            if (icon != null)
                icon.gameObject.SetActive(false);
        }

        while (upgradeIcons.Count < required)
        {
            var icon = Instantiate(upgradeIconPrefab, upgradeIconContainer);
            upgradeIcons.Add(icon);
        }
    }

    public Sprite GetIconSprite()
    {
        return itemView != null ? itemView.GetIconSprite() : null;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateHighlightMask();
    }

    public void SetHighlight(bool active, Color highlightColor)
    {
        _ = highlightColor;
        isHighlighted = active;
        UpdateHighlightMask();
    }

    void UpdateHighlightMask()
    {
        if (itemView != null)
            itemView.SetHighlight(isSelected || isHighlighted);
    }

    public void PinTooltip()
    {
        tooltipTarget?.Pin();
    }
}
