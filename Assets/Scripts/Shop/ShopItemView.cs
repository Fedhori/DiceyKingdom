using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ShopItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    public ShopItemType ViewType { get; private set; } = ShopItemType.Pin;

    Action<int> onClick;
    Action<int, Vector2> onBeginDrag;
    Action<int, Vector2> onDrag;
    Action<int, Vector2> onEndDrag;

    bool canDrag;
    bool canClick;
    bool isSelected;
    Color baseIconColor;
    bool baseColorInitialized;

    int index = -1;
    IShopItem boundItem;
    PinShopItem boundPinItem;
    TokenShopItem boundTokenItem;

    void Awake()
    {
        if (iconImage != null)
        {
            baseIconColor = iconImage.color;
            baseColorInitialized = true;
        }
    }

    public void SetIndex(int i)
    {
        index = i;
    }

    public void SetViewType(ShopItemType type)
    {
        ViewType = type;
    }

    public void SetHandlers(Action<int> click, Action<int, Vector2> beginDrag, Action<int, Vector2> drag, Action<int, Vector2> endDrag)
    {
        onClick = click;
        onBeginDrag = beginDrag;
        onDrag = drag;
        onEndDrag = endDrag;
    }

    public void SetData(IShopItem item, int price, bool canBuy, bool sold)
    {
        boundItem = item;
        boundPinItem = item as PinShopItem;
        boundTokenItem = item as TokenShopItem;
        ViewType = item != null ? item.ItemType : ViewType;

        bool canInteract = (item != null) && canBuy && !sold;
        canDrag = canInteract;
        canClick = canInteract;

        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;

            if (!baseColorInitialized)
            {
                baseIconColor = iconImage.color;
                baseColorInitialized = true;
            }

            ApplySelectionColor();
        }

        if (priceText != null)
        {
            if (sold)
            {
                priceText.text = LocalizationUtil.SoldString;
                priceText.color = Colors.Black;
            }
            else
            {
                priceText.text = $"${price}";
                priceText.color = canBuy ? Colors.Black : Colors.Red;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySelectionColor();
    }

    void ApplySelectionColor()
    {
        if (iconImage == null || !baseColorInitialized)
            return;

        if (!isSelected)
        {
            iconImage.color = baseIconColor;
            return;
        }

        float factor = 1.2f;
        var c = baseIconColor;
        c.r = Mathf.Clamp01(c.r * factor);
        c.g = Mathf.Clamp01(c.g * factor);
        c.b = Mathf.Clamp01(c.b * factor);
        iconImage.color = c;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canClick)
            return;

        onClick?.Invoke(index);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag || boundItem == null)
            return;

        if (index < 0)
            return;

        onBeginDrag?.Invoke(index, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag)
            return;

        if (index < 0)
            return;

        onDrag?.Invoke(index, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag)
            return;

        if (index < 0)
            return;

        onEndDrag?.Invoke(index, eventData.position);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    void ShowTooltip(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null || boundItem == null)
            return;

        TooltipModel model;
        if (boundPinItem != null)
        {
            model = PinTooltipUtil.BuildModel(boundPinItem.PreviewInstance);
        }
        else if (boundTokenItem != null)
        {
            model = TokenTooltipUtil.BuildModel(boundTokenItem.PreviewInstance);
        }
        else
        {
            return;
        }

        var anchor = TooltipAnchor.FromScreen(eventData.position, eventData.position);
        manager.BeginHover(this, model, anchor);
    }

    void HideTooltip()
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;
        manager.EndHover(this);
    }
}
