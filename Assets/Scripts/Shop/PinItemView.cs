using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PinItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private PinUiTooltipTarget tooltipTarget;

    Action onClick;

    PinInstance boundPin;

    bool canDrag;   // = canBuy && !sold && pin != null
    bool canClick;  // = canBuy && !sold && pin != null

    bool isSelected;
    Color baseIconColor;
    bool baseColorInitialized;

    int index = -1;

    void Awake()
    {
        if (tooltipTarget == null)
            tooltipTarget = GetComponent<PinUiTooltipTarget>();

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

    public void SetClickHandler(Action handler)
    {
        onClick = handler;
    }

    public void SetData(PinInstance pin, int price, bool canBuy, bool sold)
    {
        boundPin = pin;

        // 구매 불가 상태면 클릭/드래그 전부 막는다
        bool canInteract = (pin != null) && canBuy && !sold;
        canDrag = canInteract;
        canClick = canInteract;

        if (pin == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = SpriteCache.GetPinSprite(pin.Id);

            if (!baseColorInitialized)
            {
                baseIconColor = iconImage.color;
                baseColorInitialized = true;
            }

            ApplySelectionColor();
        }

        if (tooltipTarget != null)
            tooltipTarget.Bind(pin);

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

        onClick?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag || boundPin == null)
            return;

        if (index < 0)
            return;

        ShopManager.Instance?.BeginPinDrag(index, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag)
            return;

        if (index < 0)
            return;

        ShopManager.Instance?.UpdatePinDrag(index, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag)
            return;

        if (index < 0)
            return;

        ShopManager.Instance?.EndPinDrag(index, eventData.position);
    }
}
