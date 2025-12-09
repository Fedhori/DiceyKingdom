using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BallRewardController : MonoBehaviour
{
    [SerializeField] private Image ballIcon;
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private Button selectButton;

    string ballId;
    int ballCount;
    Action<string, int> onSelected;
    bool isInitialized;

    void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
        }
    }

    public void Initialize(string ballId, int ballCount, Action<string, int> onSelected)
    {
        this.ballId = ballId;
        this.ballCount = ballCount;
        this.onSelected = onSelected;

        if (ballIcon != null)
            ballIcon.sprite = SpriteCache.GetBallSprite(ballId);

        if (ballCountText != null)
            ballCountText.text = ballCount.ToString();

        isInitialized = true;
    }

    void HandleClick()
    {
        if (!isInitialized)
            return;

        selectButton.interactable = false;
        onSelected?.Invoke(ballId, ballCount);
    }
}
