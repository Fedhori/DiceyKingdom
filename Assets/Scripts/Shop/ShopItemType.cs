using System;
using UnityEngine;

public enum ShopItemType
{
    Pin = 0,
    Token = 1
}

[Serializable]
public struct ShopItemProbability
{
    public ShopItemType type;
    [Range(0, 100)] public int weight;
}
