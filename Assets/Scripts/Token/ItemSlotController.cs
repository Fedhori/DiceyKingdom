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

    [SerializeField] RectTransform rectTransform;
    public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
    [SerializeField] RectTransform dropArea;
    public RectTransform DropArea => dropArea != null ? dropArea : RectTransform;
    [SerializeField] ItemView itemView;
    [SerializeField] ItemTooltipTarget tooltipTarget;

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

        if (Instance != null)
            return;

        if (StageManager.Instance.CurrentPhase != StagePhase.Shop)
            return;

        var shop = ShopManager.Instance;
        if (shop == null)
            return;

        shop.TryPurchaseSelectedItemAt(SlotIndex);
    }

    public void SetSlotIndex(int index)
    {
        SlotIndex = index;
    }

    public void Bind(ItemInstance instance)
    {
        Instance = instance;
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
    }

    public Sprite GetIconSprite()
    {
        return itemView != null ? itemView.GetIconSprite() : null;
    }

    public void SetHighlight(bool active, Color highlightColor)
    {
        _ = highlightColor;
        itemView?.SetHighlight(active);
    }
}
