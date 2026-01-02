using System.Collections.Generic;
using UnityEngine;

public static class GameConfig
{
    public static int BaseIncome = 10;
    public static int ItemSlotCount = 8;

    // Player/Item base stats
    public static float PlayerBaseMoveSpeed = 500f;
    public static float ItemBaseProjectileSize = 1f;
    public static float ItemBaseProjectileSpeed = 1000f;
}

public static class LayerMaskUtil
{
    public static readonly int PinLayer = LayerMask.NameToLayer("Pin");
    public static readonly LayerMask PinMask = 1 << PinLayer;

    public static bool Contains(this LayerMask mask, GameObject go)
    {
        int bit = 1 << go.layer;
        return (mask.value & bit) != 0;
    }
}
