using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopItemView : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text priceText;
    [SerializeField] Button buyButton;

    Action onClick;

    void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleClick);
        }
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
        if (pin == null)
        {
            gameObject.SetActive(false);
            return;
        }

        var pinId = pin.Id;

        if (nameText != null)
            nameText.text = LocalizationUtil.GetPinName(pinId);

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