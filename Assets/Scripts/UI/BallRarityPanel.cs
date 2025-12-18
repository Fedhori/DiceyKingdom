using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BallRarityPanel : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject entryPrefab;

    static readonly BallRarity[] Rarities =
    {
        BallRarity.Common,
        BallRarity.Uncommon,
        BallRarity.Rare,
        BallRarity.Epic,
        BallRarity.Legendary
    };

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
        {
            Debug.LogError("[BallRarityPanel] Player not available.");
            return;
        }

        if (container == null || entryPrefab == null)
        {
            Debug.LogError("[BallRarityPanel] container or entryPrefab not set.");
            return;
        }

        Clear();
        BuildEntries(player);
    }

    void Clear()
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    void BuildEntries(PlayerInstance player)
    {
        IReadOnlyList<float> probs = player.RarityProbabilities;
        float growth = player.RarityGrowth;

        for (int i = 0; i < Rarities.Length; i++)
        {
            float prob = (probs != null && i < probs.Count) ? probs[i] : 0f;
            double multiplier = Math.Pow(growth, i);
            var rarity = Rarities[i];

            var go = Instantiate(entryPrefab, container);
            var view = go.GetComponent<BallRarityEntryView>();
            if (view == null)
            {
                Debug.LogError("[BallRarityPanel] BallRarityEntryView missing on entry prefab.");
                continue;
            }

            view.Bind(GetColorForRarity(rarity), multiplier, prob);
        }
    }

    Color GetColorForRarity(BallRarity rarity)
    {
        switch (rarity)
        {
            case BallRarity.Common:
                return Colors.Common;
            case BallRarity.Uncommon:
                return Colors.Uncommon;
            case BallRarity.Rare:
                return Colors.Rare;
            case BallRarity.Epic:
                return Colors.Epic;
            case BallRarity.Legendary:
                return Colors.Legendary;
            default:
                return Colors.Common;
        }
    }
}
