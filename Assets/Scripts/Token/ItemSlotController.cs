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
    [SerializeField] GameObject upgradeVfxRoot;

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
                Instance.OnUpgradeChanged -= HandleUpgradeChanged;

            Instance = instance;

            if (Instance != null)
                Instance.OnUpgradeChanged += HandleUpgradeChanged;
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
            bool hasUpgrade = displayInstance != null && displayInstance.Upgrade != null;
            upgradeVfxRoot.SetActive(hasUpgrade);
        }
    }

    void HandleUpgradeChanged(ItemInstance instance)
    {
        if (!ReferenceEquals(Instance, instance))
            return;

        UpdateView();
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
