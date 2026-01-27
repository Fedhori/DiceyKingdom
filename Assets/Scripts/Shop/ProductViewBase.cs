using System;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class ProductViewBase : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ProductType ViewType { get; private set; }

    protected bool CanClick { get; set; }
    protected bool CanDrag { get; set; }

    Action<int> onClick;
    Action<int, Vector2> onBeginDrag;
    Action<int, Vector2> onDrag;
    Action<int, Vector2> onEndDrag;

    int index = -1;

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

    public abstract void SetData(IProduct product, int price, bool canBuy, bool canDrag, bool sold);
    public abstract void SetSelected(bool selected);
    public abstract void PinTooltip();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!CanClick)
            return;

        onClick?.Invoke(index);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!CanDrag)
            return;

        if (index < 0)
            return;

        onBeginDrag?.Invoke(index, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!CanDrag)
            return;

        if (index < 0)
            return;

        onDrag?.Invoke(index, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!CanDrag)
            return;

        if (index < 0)
            return;

        onEndDrag?.Invoke(index, eventData.position);
    }
}
