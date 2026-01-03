using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public sealed class TooltipView : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text damageMultiplierText;
    [SerializeField] GameObject damageContainer;

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

        if (nameText != null)
            nameText.text = model.Title ?? string.Empty;

        if (descriptionText != null)
            descriptionText.text = model.Body ?? string.Empty;

        if (damageMultiplierText != null)
        {
            bool showDamage = model is { Kind: TooltipKind.Item, Damage: > 0f };
            damageMultiplierText.gameObject.SetActive(showDamage);
            if (damageContainer != null)
                damageContainer.SetActive(showDamage);
            if (showDamage)
                damageMultiplierText.text = $"{model.Damage:0.##}";
        }

        if (iconImage != null)
        {
            if (model.Icon != null)
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
