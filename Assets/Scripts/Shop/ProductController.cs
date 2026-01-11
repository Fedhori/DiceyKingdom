using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ProductController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private ItemTooltipTarget tooltipTarget;
    [SerializeField] private TMP_Text priceText;

    public ProductType ViewType { get; private set; }

    Action<int> onClick;
    Action<int, Vector2> onBeginDrag;
    Action<int, Vector2> onDrag;
    Action<int, Vector2> onEndDrag;

    bool canDrag;
    bool canClick;
    bool isSelected;

    int index = -1;
    IProduct boundProduct;
    ItemProduct boundItem;

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
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

        bool canInteract = (product != null) && !sold;
        canDrag = canBuy && canInteract;
        canClick = canInteract;

        if (product == null)
        {
            gameObject.SetActive(false);
            if (tooltipTarget != null)
                tooltipTarget.Clear();
            return;
        }

        gameObject.SetActive(true);

        if (itemView != null)
        {
            itemView.SetIcon(product.Icon);
            itemView.SetRarity(boundItem != null ? boundItem.Rarity : ItemRarity.Common);
            itemView.SetSelected(isSelected);
        }

        if (tooltipTarget != null)
        {
            if (boundItem != null)
                tooltipTarget.Bind(boundItem.PreviewInstance);
            else
                tooltipTarget.Clear();
        }

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
        itemView?.SetSelected(isSelected);
    }

    public void PinTooltip()
    {
        tooltipTarget?.Pin();
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
}
