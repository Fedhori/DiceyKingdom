using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class BallItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private BallUITooltipTarget tooltipTarget;

    // 예전 클릭용 콜백 (지금은 안 씀, 남겨만 둠)
    Action onClick;

    // 드래그용 상태
    int index = -1;
    BallDto currentBall;
    int currentBallCount;
    int currentPrice;
    bool currentSold;

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
        // 클릭으로는 더 이상 구매하지 않음
        onClick?.Invoke();
    }

    public void SetClickHandler(Action handler)
    {
        onClick = handler;
    }

    public void SetIndex(int i)
    {
        index = i;
    }

    public void SetData(BallDto ball, int ballCount, int price, bool canBuy, bool sold)
    {
        if (ball == null)
        {
            currentBall = null;
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        currentBall      = ball;
        currentBallCount = ballCount;
        currentPrice     = price;
        currentSold      = sold;

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
                priceText.text  = LocalizationUtil.SoldString;
                priceText.color = Colors.Black;
            }
            else
            {
                priceText.text  = $"${price}";
                priceText.color = canBuy ? Colors.Black : Colors.Red;
            }
        }

        if (buyButton != null)
            buyButton.interactable = !sold; // 드래그는 여전히 가능
    }

    // ===== 드래그 처리 =====

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentBall == null || currentSold)
            return;

        ShopManager.Instance?.BeginBallDrag(index, currentBall, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentBall == null || currentSold)
            return;

        ShopManager.Instance?.UpdateBallDrag(index, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentBall == null || currentSold)
            return;

        ShopManager.Instance?.EndBallDrag(index, eventData.position);
    }
}
