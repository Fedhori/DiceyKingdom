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
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        // 툴팁의 "좌상단"이 pivot 이 되도록 강제
        if (rectTransform != null)
            rectTransform.pivot = new Vector2(0f, 1f);

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
            nameText.text = LocalizationUtil.GetPinName(pin.Id);

        if (iconImage != null)
            iconImage.sprite = SpriteCache.GetPinSprite(pin.Id);

        if (descriptionText != null)
        {
            // TODO - 효과마다 그에 맞는 설명 문구를 만들어야 함
            float mult = pin.ScoreMultiplier;
            descriptionText.text = $"Score x{mult:0.##}";
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}