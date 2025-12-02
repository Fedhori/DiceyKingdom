using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TooltipView : MonoBehaviour
{
    [Header("Icon + Text")]
    [SerializeField] GameObject iconBlockRoot;      // 아이콘 들어가는 패널 전체 루트
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;

    public RectTransform rectTransform;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
            rectTransform.pivot = new Vector2(0f, 1f);

        gameObject.SetActive(false);
    }

    public void Show(TooltipModel model)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        gameObject.SetActive(true);

        // Kind 에 따라 아이콘 블럭 온/오프
        bool showIconBlock = model.Kind == TooltipKind.Pin || model.Kind == TooltipKind.Ball;

        if (iconBlockRoot != null)
            iconBlockRoot.SetActive(showIconBlock);

        if (nameText != null)
            nameText.text = model.Title ?? string.Empty;

        if (descriptionText != null)
            descriptionText.text = model.Body ?? string.Empty;

        if (iconImage != null)
        {
            if (showIconBlock && model.Icon != null)
            {
                iconImage.sprite = model.Icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}