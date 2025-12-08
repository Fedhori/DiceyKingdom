using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PinItemView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text priceText;
    [SerializeField] Button buyButton;
    [SerializeField] PinUiTooltipTarget tooltipTarget;

    Action onClick;
    Color defaultBackgroundColor = Color.white;

    void Awake()
    {
        if (background != null)
            defaultBackgroundColor = background.color;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleClick);
        }

        if (tooltipTarget == null)
            tooltipTarget = GetComponent<PinUiTooltipTarget>();
    }

    void HandleClick()
    {
        onClick?.Invoke();
    }

    public void SetClickHandler(Action handler)
    {
        onClick = handler;
    }

    public void SetData(PinInstance pin, int price, bool canBuy, bool sold)
    {
        // 툴팁 쪽에도 현재 핀 정보 전달
        if (tooltipTarget != null)
            tooltipTarget.Bind(pin);

        if (pin == null)
        {
            gameObject.SetActive(false);
            return;
        }

        var pinId = pin.Id;

        if (iconImage != null)
            iconImage.sprite = SpriteCache.GetPinSprite(pinId);

        if (priceText != null)
        {
            if (sold)
            {
                priceText.text = LocalizationUtil.SoldString;
            }
            else
            {
                priceText.text = $"${price}";
                priceText.color = canBuy ? Colors.Black : Colors.Red;
            }
        }

        if (buyButton != null)
            buyButton.interactable = !sold;
    }

    public void SetSelected(bool selected)
    {
        if (background == null)
            return;

        background.color = selected ? Colors.HighlightColor : defaultBackgroundColor;
    }
}
