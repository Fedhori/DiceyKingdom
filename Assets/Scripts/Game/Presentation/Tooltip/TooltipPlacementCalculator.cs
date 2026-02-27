using UnityEngine;

namespace Game.UI.Tooltip
{
    public static class TooltipPlacementCalculator
    {
        public static Vector2 ComputeScreenTopLeft(
            TooltipAnchor anchor,
            Vector2 tooltipSize,
            Vector2 screenSize,
            Vector2 offset,
            float edgePadding)
        {
            float tooltipWidth = Mathf.Max(0f, tooltipSize.x);
            float tooltipHeight = Mathf.Max(0f, tooltipSize.y);
            float padding = Mathf.Max(0f, edgePadding);
            float screenWidth = Mathf.Max(0f, screenSize.x);
            float screenHeight = Mathf.Max(0f, screenSize.y);

            Vector2 rightTop = anchor.ScreenRightTop;
            Vector2 leftTop = anchor.ScreenLeftTop;

            float x = rightTop.x + offset.x;
            bool canPlaceRight = x + tooltipWidth <= screenWidth - padding;
            if (!canPlaceRight)
            {
                x = leftTop.x - offset.x - tooltipWidth;
            }

            float y = rightTop.y + offset.y;

            if (y > screenHeight - padding)
            {
                y = screenHeight - padding;
            }

            if (y - tooltipHeight < padding)
            {
                y = padding + tooltipHeight;
            }

            float minX = padding;
            float maxX = screenWidth - padding - tooltipWidth;
            if (maxX < minX)
            {
                maxX = minX;
            }

            x = Mathf.Clamp(x, minX, maxX);
            return new Vector2(x, y);
        }
    }
}
