using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionOverlayTestDiceView : MonoBehaviour
{
    [SerializeField] TMP_Text valueText;
    [SerializeField] TMP_Text clearedCheckText;
    [SerializeField] TMP_Text rightArrowText;
    [SerializeField] Transform abilityIconRoot;
    [SerializeField] Image abilityIconPrefab;

    readonly List<Image> iconPool = new();
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    public void SetData(MissionOverlayTestDiceData data, MissionIconRegistry iconRegistry)
    {
        if (!setupValid || data == null || iconRegistry == null)
            return;

        valueText.text = Mathf.Max(0, data.value).ToString();
        if (clearedCheckText != null)
            clearedCheckText.gameObject.SetActive(data.isCleared);
        if (rightArrowText != null)
            rightArrowText.gameObject.SetActive(data.showRightArrow);

        int iconCount = data.requiredAbilities?.Count ?? 0;
        GrowIconPool(iconCount);
        for (int i = 0; i < iconPool.Count; i++)
        {
            bool active = i < iconCount;
            Image icon = iconPool[i];
            icon.gameObject.SetActive(active);
            if (!active)
                continue;

            string abilityId = data.requiredAbilities[i];
            if (!iconRegistry.TryResolveAbilityIcon(abilityId, out Sprite sprite, out Color color))
            {
                icon.enabled = false;
                continue;
            }

            icon.enabled = true;
            icon.sprite = sprite;
            icon.color = color;
        }
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (valueText == null)
        {
            Debug.LogError("[MissionOverlay] valueText is not assigned.", this);
            valid = false;
        }

        if (clearedCheckText == null)
        {
            Debug.LogError("[MissionOverlay] clearedCheckText is not assigned.", this);
            valid = false;
        }

        if (rightArrowText == null)
        {
            Debug.LogError("[MissionOverlay] rightArrowText is not assigned.", this);
            valid = false;
        }

        if (abilityIconRoot == null)
        {
            Debug.LogError("[MissionOverlay] abilityIconRoot is not assigned.", this);
            valid = false;
        }

        if (abilityIconPrefab == null)
        {
            Debug.LogError("[MissionOverlay] abilityIconPrefab is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void GrowIconPool(int requiredCount)
    {
        while (iconPool.Count < requiredCount)
        {
            Image created = Instantiate(abilityIconPrefab, abilityIconRoot);
            created.gameObject.SetActive(true);
            iconPool.Add(created);
        }
    }
}
