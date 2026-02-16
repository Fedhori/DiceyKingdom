using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public sealed class MissionWorldTestRowView : MonoBehaviour
{
    [SerializeField] Transform abilityIconRoot;
    [SerializeField] UnityEngine.UI.Image abilityIconPrefab;
    [SerializeField] TMP_Text difficultyText;
    [SerializeField] TMP_Text clearedStateText;
    readonly List<UnityEngine.UI.Image> iconPool = new();
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    public void SetData(MissionWorldTestData data, MissionIconRegistry iconRegistry)
    {
        if (!setupValid || data == null || iconRegistry == null)
            return;

        difficultyText.text = Mathf.Max(0, data.difficulty).ToString(CultureInfo.InvariantCulture);
        clearedStateText.text = "✓";
        clearedStateText.gameObject.SetActive(data.isCleared);

        int iconCount = data.requiredAbilities?.Count ?? 0;
        GrowIconPool(iconCount);

        for (int i = 0; i < iconPool.Count; i++)
        {
            bool active = i < iconCount;
            UnityEngine.UI.Image icon = iconPool[i];
            icon.gameObject.SetActive(active);
            if (!active)
                continue;

            string abilityId = data.requiredAbilities[i];
            if (iconRegistry.TryResolveAbilityIcon(abilityId, out Sprite sprite, out Color color))
            {
                icon.enabled = true;
                icon.sprite = sprite;
                icon.color = color;
            }
            else
            {
                icon.enabled = false;
            }
        }
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (abilityIconRoot == null)
        {
            Debug.LogError("[MissionWorld] abilityIconRoot is not assigned.", this);
            valid = false;
        }

        if (abilityIconPrefab == null)
        {
            Debug.LogError("[MissionWorld] abilityIconPrefab is not assigned.", this);
            valid = false;
        }

        if (difficultyText == null)
        {
            Debug.LogError("[MissionWorld] difficultyText is not assigned.", this);
            valid = false;
        }

        if (clearedStateText == null)
        {
            Debug.LogError("[MissionWorld] clearedStateText is not assigned.", this);
        }

        return valid;
    }

    void GrowIconPool(int requiredCount)
    {
        while (iconPool.Count < requiredCount)
        {
            UnityEngine.UI.Image created = Instantiate(abilityIconPrefab, abilityIconRoot);
            created.gameObject.SetActive(true);
            iconPool.Add(created);
        }
    }
}
