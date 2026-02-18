using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionOverlayStatDiceView : MonoBehaviour
{
    [SerializeField] TMP_Text valueText;
    [SerializeField] Image abilityIconImage;
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    public void SetData(MissionOverlayStatDiceData data, MissionIconRegistry iconRegistry)
    {
        if (!setupValid || data == null || iconRegistry == null)
            return;

        valueText.text = Mathf.Max(0, data.value).ToString();
        if (!iconRegistry.TryResolveAbilityIcon(data.abilityId, out Sprite sprite, out Color color))
        {
            abilityIconImage.enabled = false;
            return;
        }

        abilityIconImage.enabled = true;
        abilityIconImage.sprite = sprite;
        abilityIconImage.color = color;
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (valueText == null)
        {
            Debug.LogError("[MissionOverlay] valueText is not assigned.", this);
            valid = false;
        }

        if (abilityIconImage == null)
        {
            Debug.LogError("[MissionOverlay] abilityIconImage is not assigned.", this);
            valid = false;
        }

        return valid;
    }
}
