using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class TokenController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int SlotIndex { get; private set; } = -1;
    public TokenInstance Instance { get; private set; }

    [SerializeField] RectTransform rectTransform;
    public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
    [SerializeField] Image iconImage;
    [SerializeField] Image highlightImage;
    [SerializeField] Graphic raycastGraphic;
    [SerializeField] TooltipAnchorType anchorType = TooltipAnchorType.Screen;

    Color baseHighlightColor = Color.white;

    void Awake()
    {
        if (raycastGraphic == null)
            raycastGraphic = highlightImage != null ? highlightImage : (iconImage != null ? iconImage : GetComponent<Graphic>());

        if (raycastGraphic != null)
            raycastGraphic.raycastTarget = true;

        if (highlightImage != null)
            baseHighlightColor = highlightImage.color;

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

        var flow = FlowManager.Instance;
        if (flow != null && flow.CurrentPhase != FlowPhase.Shop)
            return;

        var shop = ShopManager.Instance;
        if (shop == null)
            return;

        shop.TryPurchaseSelectedTokenAt(SlotIndex);
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
        if (Instance == null)
            return;

        TokenManager.Instance?.BeginDrag(this);
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

        iconImage.sprite = SpriteCache.GetTokenSprite(Instance?.Id);
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
        if (highlightImage == null)
            return;

        highlightImage.color = active ? highlightColor : baseHighlightColor;
    }

    public void ShowTooltip(PointerEventData eventData)
    {
        if (Instance == null)
            return;

        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        TooltipModel model = TokenTooltipUtil.BuildModel(Instance);
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
