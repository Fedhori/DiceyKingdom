using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionWorldTestRowView : MonoBehaviour
{
    [SerializeField] Transform abilityIconRoot;
    [SerializeField] Image abilityIconPrefab;
    [SerializeField] TMP_Text difficultyText;
    [SerializeField] TMP_Text clearedStateText;
    readonly List<Image> iconPool = new();
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

        difficultyText.text = $"D{Mathf.Max(0, data.difficulty)}";
        clearedStateText.text = data.isCleared ? "Y" : "N";
        clearedStateText.color = data.isCleared ? Colors.Semantic.MissionTestCleared : Colors.Semantic.MissionTestPending;

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
            Debug.LogError("[MissionWorldTestRowView] abilityIconRoot is not assigned.", this);
            valid = false;
        }

        if (abilityIconPrefab == null)
        {
            Debug.LogError("[MissionWorldTestRowView] abilityIconPrefab is not assigned.", this);
            valid = false;
        }

        if (difficultyText == null)
        {
            Debug.LogError("[MissionWorldTestRowView] difficultyText is not assigned.", this);
            valid = false;
        }

        if (clearedStateText == null)
        {
            Debug.LogError("[MissionWorldTestRowView] clearedStateText is not assigned.", this);
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
