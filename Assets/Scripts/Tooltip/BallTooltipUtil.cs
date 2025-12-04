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

        // 이름은 프로젝트 상황에 맞게 구현해 둔다.
        // LocalizationUtil.GetBallName(id)가 없다면, 일단 id 그대로 쓰고 나중에 교체해도 된다.
        string title = LocalizationUtil.GetBallName(id);  // 필요시 구현
        Sprite icon = SpriteCache.GetBallSprite(id);

        // TODO - 임시임. 볼 효과 구현 시작하며 그에 맞게 로컬라이징 필요
        string body = $"점수 배율 x{ball.BallScoreMultiplier:0.##}";

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Ball,
            0
        );
    }
}