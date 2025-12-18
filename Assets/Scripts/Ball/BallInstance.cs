using System;
using UnityEngine;

public enum BallRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public sealed class BallInstance
{
    public BallRarity Rarity { get; }
    public double ScoreMultiplier { get; private set; }
    public double CriticalMultiplier => 1f;

    public float PendingSpeedFactor { get; set; } = 1f;
    public float PendingSizeFactor { get; set; } = 1f;

    public Color RarityColor { get; }

    // TODO: life 매커니즘 제거 예정. 남아있는 핀 효과 대비하여 유지.
    public int life = 0;

    public BallInstance(BallRarity rarity, float rarityGrowth)
    {
        if (rarityGrowth <= 0f)
        {
            Debug.LogError($"[BallInstance] Invalid rarityGrowth: {rarityGrowth}");
            rarityGrowth = 1f;
        }

        Rarity = rarity;
        ScoreMultiplier = Math.Pow(rarityGrowth, (int)rarity);
        RarityColor = GetColorForRarity(rarity);
    }

    public void SetRarityGrowth(float rarityGrowth)
    {
        if (rarityGrowth <= 0f)
        {
            Debug.LogError($"[BallInstance] Invalid rarityGrowth: {rarityGrowth}");
            return;
        }

        ScoreMultiplier = Math.Pow(rarityGrowth, (int)Rarity);
    }

    public void OnHitPin(PinInstance pin, Vector2 position)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }

        ScoreManager.Instance.CalculateScore(this, pin, position);
    }

    public void OnHitBall(BallInstance other, Vector2 position)
    {
        // 현재 희귀도 기반 로직에서는 추가 효과 없음
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
