using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BallItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private BallUITooltipTarget tooltipTarget;

    int index = -1;

    BallDto boundBall;
    int boundBallCount;
    int boundPrice;
    bool canDrag;   // = canBuy && !sold && ball != null

    void Awake()
    {
        if (tooltipTarget == null)
            tooltipTarget = GetComponent<BallUITooltipTarget>();
    }

    public void SetIndex(int i)
    {
        index = i;
    }

    public void SetData(BallDto ball, int ballCount, int price, bool canBuy, bool sold)
    {
        boundBall = ball;
        boundBallCount = ballCount;
        boundPrice = price;

        // 구매 불가 상태면 드래그 자체를 막는다
        canDrag = (ball != null) && canBuy && !sold;

        if (ball == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (ballCountText != null)
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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag || boundBall == null)
            return;

        if (index < 0)
            return;

        ShopManager.Instance?.BeginBallDrag(index, boundBall, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag)
            return;

        if (index < 0)
            return;

        ShopManager.Instance?.UpdateBallDrag(index, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!canDrag)
            return;

        if (index < 0)
            return;

        ShopManager.Instance?.EndBallDrag(index, eventData.position);
    }
}
