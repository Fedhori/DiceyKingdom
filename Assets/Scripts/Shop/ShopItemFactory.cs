using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class ShopItemFactory : MonoBehaviour
{
    public static ShopItemFactory Instance { get; private set; }

    System.Random rng;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rng = GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();
    }

    public ShopItemType RollType(IReadOnlyList<ShopItemProbability> probabilities)
    {
        if (probabilities == null || probabilities.Count == 0)
            return ShopItemType.Pin;

        int totalWeight = 0;
        for (int i = 0; i < probabilities.Count; i++)
            totalWeight += Mathf.Max(0, probabilities[i].weight);

        if (totalWeight <= 0)
            return ShopItemType.Pin;

        int roll = rng.Next(0, totalWeight);
        int acc = 0;
        for (int i = 0; i < probabilities.Count; i++)
        {
            acc += Mathf.Max(0, probabilities[i].weight);
            if (roll < acc)
                return probabilities[i].type;
        }

        return probabilities[probabilities.Count - 1].type;
    }

    public IShopItem CreatePin(PinDto dto)
    {
        if (dto == null)
            return null;

        return new PinShopItem(dto);
    }

    public IShopItem CreateToken(TokenDto dto)
    {
        if (dto == null)
            return null;

        return new TokenShopItem(dto);
    }
}
