using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ProductView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text priceText;

    public ProductType ViewType { get; private set; }

    Action<int> onClick;
    Action<int, Vector2> onBeginDrag;
    Action<int, Vector2> onDrag;
    Action<int, Vector2> onEndDrag;

    bool canDrag;
    bool canClick;
    bool isSelected;
    Color baseIconColor;
    bool baseColorInitialized;
    Color baseBackgroundColor;
    bool baseBackgroundInitialized;

    int index = -1;
    IProduct boundProduct;
    ItemProduct boundItem;

    void Awake()
    {
        if (iconImage != null)
        {
            baseIconColor = iconImage.color;
            baseColorInitialized = true;
        }

        if (backgroundImage != null)
        {
            baseBackgroundColor = backgroundImage.color;
            baseBackgroundInitialized = true;
        }
    }

    public void SetIndex(int i)
    {
        index = i;
    }

    public void SetViewType(ProductType type)
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

    public void SetData(IProduct product, int price, bool canBuy, bool sold)
    {
        boundProduct = product;
        boundItem = product as ItemProduct;
        ViewType = product?.ProductType ?? ViewType;

        bool canInteract = (product != null) && canBuy && !sold;
        canDrag = canInteract;
        canClick = canInteract;

        if (product == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = product.Icon;

            if (!baseColorInitialized)
            {
                baseIconColor = iconImage.color;
                baseColorInitialized = true;
            }

            ApplySelectionColor();
        }

        ApplyBackgroundColor();

        if (priceText != null)
        {
            if (sold)
            {
                priceText.text = LocalizationUtil.SoldString;
                priceText.color = Colors.White;
            }
            else
            {
                priceText.text = $"${price}";
                priceText.color = canBuy ? Colors.White : Colors.Red;
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

    void ApplyBackgroundColor()
    {
        if (backgroundImage == null)
            return;

        if (!baseBackgroundInitialized)
        {
            baseBackgroundColor = backgroundImage.color;
            baseBackgroundInitialized = true;
        }

        if (boundItem == null)
        {
            backgroundImage.color = baseBackgroundColor;
            return;
        }

        backgroundImage.color = Colors.GetRarityColor(boundItem.Rarity);
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

        if (!canDrag || boundProduct == null)
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
        if (manager == null || boundProduct == null)
            return;

        if (boundItem == null)
            return;

        var model = ItemTooltipUtil.BuildModel(boundItem.PreviewInstance);

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
