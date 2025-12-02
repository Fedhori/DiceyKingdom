using UnityEngine;
using Data; // PinInstance 가 여기 있지 않다면 이 using 은 지워도 됨.

public static class PinTooltipUtil
{
    public static TooltipModel BuildModel(PinInstance pin)
    {
        if (pin == null)
        {
            return new TooltipModel(
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Pin
            );
        }

        string pinId = pin.Id;

        string title = LocalizationUtil.GetPinName(pinId);
        Sprite icon = SpriteCache.GetPinSprite(pinId);

        float mult = pin.ScoreMultiplier;
        string body = $"Score x{mult:0.##}";

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Pin
        );
    }
}