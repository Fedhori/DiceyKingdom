using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BallItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private BallUITooltipTarget tooltipTarget;

    Action onClick;

    void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleClick);
        }

        if (tooltipTarget == null)
            tooltipTarget = GetComponent<BallUITooltipTarget>();
    }

    void HandleClick()
    {
        onClick?.Invoke();
    }

    public void SetClickHandler(Action handler)
    {
        onClick = handler;
    }

    public void SetData(BallDto ball, int ballCount, int price, bool canBuy, bool sold)
    {
        if (ball == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ballCountText.text = $"x{ballCount}";

        if (iconImage != null)
            iconImage.sprite = SpriteCache.GetBallSprite(ball.id);

        if (tooltipTarget != null)
            tooltipTarget.Bind(new BallInstance(ball));

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

        if (buyButton != null)
            buyButton.interactable = !sold;
    }
}