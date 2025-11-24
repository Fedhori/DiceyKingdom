using Data;
using UnityEngine;

public sealed class BallInstance
{
    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;
    public int BaseScore => BaseDto.baseScore;

    public int PersonalScore { get; private set; }

    public BallInstance(BallDto dto)
    {
        BaseDto = dto;
        PersonalScore = 0;
    }

    public void OnHitNail(NailInstance nail, Vector2 position)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }

        var gained = BaseScore;
        PersonalScore += gained;
        ScoreManager.Instance.AddScore(gained, position);
    }

    public void OnHitBall(BallInstance other)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }
    }
}