using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ItemSlotController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int SlotIndex { get; private set; } = -1;
    public TokenInstance Instance { get; private set; }

    [SerializeField] RectTransform rectTransform;
    public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
    [SerializeField] Image iconImage;
    public GameObject dragHighlightMask;
    [SerializeField] TooltipAnchorType anchorType = TooltipAnchorType.Screen;

    void Awake()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
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

    public void Bind(TokenInstance instance)
    {
        Instance = instance;
        UpdateView();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Instance == null)
            return;

        TokenManager.Instance?.BeginDrag(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Instance == null)
            return;

        TokenManager.Instance?.EndDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        TokenManager.Instance?.UpdateDrag(this, eventData.position);
    }

    void UpdateView()
    {
        if (iconImage == null)
            return;

        iconImage.gameObject.SetActive(Instance != null);

        iconImage.sprite = SpriteCache.GetItemSprite(Instance?.Id);
    }

    public void SetIconVisible(bool visible)
    {
        if (iconImage != null)
            iconImage.enabled = visible;
    }

    public Sprite GetIconSprite()
    {
        return iconImage != null ? iconImage.sprite : null;
    }

    public void SetHighlight(bool active, Color highlightColor)
    {
        // Deprecated: visual highlight now handled via dragHighlightMask
    }

    public void ShowTooltip(PointerEventData eventData)
    {
        if (Instance == null)
            return;

        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        TooltipModel model = ItemTooltipUtil.BuildModel(Instance);
        TooltipAnchor anchor = anchorType == TooltipAnchorType.World
            ? TooltipAnchor.FromWorld(transform.position)
            : TooltipAnchor.FromScreen(eventData.position, eventData.position);

        manager.BeginHover(this, model, anchor);
    }

    public void HideTooltip()
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }
}
