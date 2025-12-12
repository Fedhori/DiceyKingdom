using Data;
using UnityEngine;

public class BallMainMenuController : MonoBehaviour
{
    BallDto ballDto;
    [SerializeField] SpriteRenderer ballIcon;

    void Awake()
    {
        ballDto = BallRepository.GetRandomBall();
        ballIcon.sprite = SpriteCache.GetBallSprite(ballDto.id);
    }
}