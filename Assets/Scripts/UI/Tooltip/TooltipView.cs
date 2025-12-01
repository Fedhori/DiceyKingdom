using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TooltipView : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;

    public RectTransform rectTransform;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(PinInstance pin)
    {
        if (pin == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = pin.Id;

        if (iconImage != null)
            iconImage.sprite = SpriteCache.GetPinSprite(pin.Id);

        if (descriptionText != null)
        {
            // 임시 설명: 점수 배율만 간단히 표시
            // 나중에 PinDto에 displayName / description 추가해서 교체하면 됨.
            float mult = pin.ScoreMultiplier;
            descriptionText.text = $"Score x{mult:0.##}";
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}