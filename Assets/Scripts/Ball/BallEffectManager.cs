using UnityEngine;

public sealed class BallEffectManager : MonoBehaviour
{
    public static BallEffectManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void ApplyEffect(BallEffectDto dto, BallInstance self, BallInstance otherBall, PinInstance pin, Vector2 position)
    {
        // TODO: 실제 효과 실행 로직은 이후 단계(2.3)에서 구현
    }
}
