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

        string id = ball.Id;

        string title = LocalizationUtil.GetBallName(id);
        Sprite icon = SpriteCache.GetBallSprite(id);
        
        // TODO - ball 효과를 구현하면서 이쪽도 대응 필요
        string body = "";

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Ball,
            ball.BallScoreMultiplier
        );
    }
}