using UnityEngine;

public static class BallTooltipUtil
{
    public static TooltipModel BuildModel(BallInstance ball)
    {
        if (ball == null)
        {
            return new TooltipModel(
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Ball,
                0
            );
        }

        string title = $"Ball ({ball.Rarity})";
        Sprite icon = null; // 프리팹 기본 스프라이트 사용
        string body = string.Empty;

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Ball,
            ball.ScoreMultiplier
        );
    }
}
