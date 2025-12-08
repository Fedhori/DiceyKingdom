using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopItemView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text priceText;
    [SerializeField] Button buyButton;
    [SerializeField] PinShopTooltipTarget tooltipTarget;

    Action onClick;

    void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleClick);
        }

        if (tooltipTarget == null)
            tooltipTarget = GetComponent<PinShopTooltipTarget>();
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
}