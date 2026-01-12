using System;
using UnityEngine;

public enum ProductType
{
    Item = 0,
    Upgrade = 1
}

[Serializable]
public struct ProductProbability
{
    public ProductType type;
    [Range(0, 100)] public int weight;
}
