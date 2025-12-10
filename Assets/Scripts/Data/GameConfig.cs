using System.Collections.Generic;
using UnityEngine;

public static class GameConfig
{
    public static string BasicPinId = "pin.basic";
    public static string BasicBallId = "ball.basic";
    public static int BaseRoundIncome = 10;
    public static float MaxBallPrice = 10;
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