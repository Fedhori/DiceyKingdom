using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MissionAdventurerRowView : MonoBehaviour, IPointerClickHandler
{
    const string StrengthAbilityId = "strength";
    const string AgilityAbilityId = "agility";
    const string IntelligenceAbilityId = "intelligence";

    [SerializeField] Button rowButton;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image rowBackgroundImage;
    [SerializeField] Image rowBorderImage;
    [SerializeField] Image portraitImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text staminaText;
    [SerializeField] Image strengthIconImage;
    [SerializeField] TMP_Text strengthValueText;
    [SerializeField] Image agilityIconImage;
    [SerializeField] TMP_Text agilityValueText;
    [SerializeField] Image intelligenceIconImage;
    [SerializeField] TMP_Text intelligenceValueText;
    [SerializeField] TMP_Text disabledStateText;
    [SerializeField] Image disabledOverlay;

    Action<string> onLeftClicked;
    Action<string> onRightClicked;
    string adventurerUid = string.Empty;
    bool isAssignable;
    bool setupValid;

    public string AdventurerUid => adventurerUid;
    public bool IsAssignable => isAssignable;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
        {
            enabled = false;
            return;
        }

        rowButton.onClick.AddListener(HandleLeftClicked);
    }

    public void SetData(
        MissionAdventurerRowData data,
        MissionIconRegistry iconRegistry,
        Action<string> leftClickHandler,
        Action<string> rightClickHandler)
    {
        if (!setupValid || data == null || iconRegistry == null)
            return;

        onLeftClicked = leftClickHandler;
        onRightClicked = rightClickHandler;
        adventurerUid = data.adventurerUid ?? string.Empty;
        isAssignable = data.isAssignable;

        nameText.text = string.IsNullOrWhiteSpace(data.displayName) ? "모험가" : data.displayName;
        levelText.text = $"Lv.{Mathf.Max(1, data.level)}";
        hpText.text = $"HP {Mathf.Max(0, data.hp)}/{Mathf.Max(0, data.maxHp)}";
        staminaText.text = $"STA {Mathf.Max(0, data.stamina)}/{Mathf.Max(0, data.maxStamina)}";
        strengthValueText.text = Mathf.Max(0, data.strength).ToString();
        agilityValueText.text = Mathf.Max(0, data.agility).ToString();
        intelligenceValueText.text = Mathf.Max(0, data.intelligence).ToString();

        bool hasPortrait = data.portraitSprite != null;
        portraitImage.enabled = hasPortrait;
        if (hasPortrait)
            portraitImage.sprite = data.portraitSprite;

        BindAbilityIcon(iconRegistry, StrengthAbilityId, strengthIconImage);
        BindAbilityIcon(iconRegistry, AgilityAbilityId, agilityIconImage);
        BindAbilityIcon(iconRegistry, IntelligenceAbilityId, intelligenceIconImage);

        if (disabledStateText != null)
        {
            disabledStateText.text = "배치 불가";
            disabledStateText.gameObject.SetActive(!isAssignable);
        }

        if (disabledOverlay != null)
            disabledOverlay.enabled = !isAssignable;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        rowButton.interactable = true;

        ApplyTheme();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!setupValid || eventData == null)
            return;

        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (string.IsNullOrWhiteSpace(adventurerUid))
            return;

        onRightClicked?.Invoke(adventurerUid);
    }

    void HandleLeftClicked()
    {
        if (string.IsNullOrWhiteSpace(adventurerUid))
            return;

        onLeftClicked?.Invoke(adventurerUid);
    }

    void BindAbilityIcon(MissionIconRegistry iconRegistry, string abilityId, Image iconImage)
    {
        if (iconImage == null)
            return;

        if (iconRegistry.TryResolveAbilityIcon(abilityId, out Sprite sprite, out Color color))
        {
            iconImage.enabled = true;
            iconImage.sprite = sprite;
            iconImage.color = color;
            return;
        }

        iconImage.enabled = false;
    }

    void ApplyTheme()
    {
        Color32 foreground = isAssignable
            ? Colors.Semantic.TextOnLightPrimary
            : Colors.Semantic.TextOnLightDisabled;

        Color32 secondary = isAssignable
            ? Colors.Semantic.TextOnLightSecondary
            : Colors.Semantic.TextOnLightDisabled;

        if (nameText != null)
            nameText.color = foreground;
        if (levelText != null)
            levelText.color = secondary;
        if (hpText != null)
            hpText.color = foreground;
        if (staminaText != null)
            staminaText.color = foreground;
        if (strengthValueText != null)
            strengthValueText.color = secondary;
        if (agilityValueText != null)
            agilityValueText.color = secondary;
        if (intelligenceValueText != null)
            intelligenceValueText.color = secondary;
        if (disabledStateText != null)
            disabledStateText.color = Colors.Semantic.TextOnLightDisabled;

        if (rowBackgroundImage != null)
            rowBackgroundImage.color = isAssignable
                ? Colors.Semantic.SurfaceParchmentAlt
                : Colors.Semantic.SurfaceParchmentMuted;

        if (rowBorderImage != null)
            rowBorderImage.color = Colors.Semantic.BorderParchment;

        if (disabledOverlay != null)
            disabledOverlay.color = Colors.Semantic.SurfaceParchmentMuted;
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (rowButton == null)
        {
            Debug.LogError("[AdventurerList] rowButton is not assigned.", this);
            valid = false;
        }

        if (canvasGroup == null)
        {
            Debug.LogError("[AdventurerList] canvasGroup is not assigned.", this);
            valid = false;
        }

        if (rowBackgroundImage == null)
        {
            Debug.LogError("[AdventurerList] rowBackgroundImage is not assigned.", this);
            valid = false;
        }

        if (rowBorderImage == null)
        {
            Debug.LogError("[AdventurerList] rowBorderImage is not assigned.", this);
            valid = false;
        }

        if (portraitImage == null)
        {
            Debug.LogError("[AdventurerList] portraitImage is not assigned.", this);
            valid = false;
        }

        if (nameText == null)
        {
            Debug.LogError("[AdventurerList] nameText is not assigned.", this);
            valid = false;
        }

        if (levelText == null)
        {
            Debug.LogError("[AdventurerList] levelText is not assigned.", this);
            valid = false;
        }

        if (hpText == null)
        {
            Debug.LogError("[AdventurerList] hpText is not assigned.", this);
            valid = false;
        }

        if (staminaText == null)
        {
            Debug.LogError("[AdventurerList] staminaText is not assigned.", this);
            valid = false;
        }

        if (strengthIconImage == null)
        {
            Debug.LogError("[AdventurerList] strengthIconImage is not assigned.", this);
            valid = false;
        }

        if (strengthValueText == null)
        {
            Debug.LogError("[AdventurerList] strengthValueText is not assigned.", this);
            valid = false;
        }

        if (agilityIconImage == null)
        {
            Debug.LogError("[AdventurerList] agilityIconImage is not assigned.", this);
            valid = false;
        }

        if (agilityValueText == null)
        {
            Debug.LogError("[AdventurerList] agilityValueText is not assigned.", this);
            valid = false;
        }

        if (intelligenceIconImage == null)
        {
            Debug.LogError("[AdventurerList] intelligenceIconImage is not assigned.", this);
            valid = false;
        }

        if (intelligenceValueText == null)
        {
            Debug.LogError("[AdventurerList] intelligenceValueText is not assigned.", this);
            valid = false;
        }

        if (disabledStateText == null)
        {
            Debug.LogError("[AdventurerList] disabledStateText is not assigned.", this);
            valid = false;
        }

        if (disabledOverlay == null)
        {
            Debug.LogError("[AdventurerList] disabledOverlay is not assigned.", this);
            valid = false;
        }

        return valid;
    }
}
