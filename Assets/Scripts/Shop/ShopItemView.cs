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

    public void SetData(string pinId, int price, bool canBuy, bool sold)
    {
        if (nameText != null)
            nameText.text = pinId;

        if (iconImage != null)
            iconImage.sprite = SpriteCache.GetPinSprite(pinId);

        if (priceText != null)
        {
            if (sold)
            {
                priceText.text = "SOLD";
                priceText.color = Colors.Black;
            }
            else
            {
                priceText.text = $"리롤 비용: {price}";
                priceText.color = canBuy ? Colors.Common : Colors.Red;
            }
        }

        if (buyButton != null)
            buyButton.interactable = !sold;
    }
}