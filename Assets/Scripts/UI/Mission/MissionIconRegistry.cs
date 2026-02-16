using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MissionIconRegistry : MonoBehaviour
{
    [SerializeField] Sprite strengthIcon;
    [SerializeField] Sprite agilityIcon;
    [SerializeField] Sprite intelligenceIcon;
    readonly HashSet<string> missingAbilityLogged = new(StringComparer.Ordinal);

    public bool TryResolveAbilityIcon(string abilityId, out Sprite sprite, out Color color)
    {
        sprite = null;
        color = Color.white;

        if (string.Equals(abilityId, "strength", StringComparison.Ordinal))
        {
            sprite = strengthIcon;
        }
        else if (string.Equals(abilityId, "agility", StringComparison.Ordinal))
        {
            sprite = agilityIcon;
        }
        else if (string.Equals(abilityId, "intelligence", StringComparison.Ordinal))
        {
            sprite = intelligenceIcon;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(abilityId) && missingAbilityLogged.Add(abilityId))
                Debug.LogError($"[MissionWorld] Unknown ability id: {abilityId}", this);
            return false;
        }

        if (sprite == null)
        {
            if (!string.IsNullOrWhiteSpace(abilityId) && missingAbilityLogged.Add($"sprite:{abilityId}"))
                Debug.LogError($"[MissionWorld] Missing sprite for ability id: {abilityId}", this);
            return false;
        }

        return true;
    }
}
