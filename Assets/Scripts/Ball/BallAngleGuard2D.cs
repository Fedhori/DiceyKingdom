using UnityEngine;

public sealed class BallAngleGuard2D : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    private readonly float minAbsY = GameConfig.BallSpeed * 0.2f;

    void Reset() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        var v = rb.linearVelocity;

        if (v.sqrMagnitude < 0.0001f) return;

        // 1) 각도 가드
        v = ApplyMinComponent(v);

        // 2) 속도 고정(벽돌깨기는 보통 고정이 더 안정적임)
        v = v.normalized * GameConfig.BallSpeed;

        rb.linearVelocity = v;
    }

    Vector2 ApplyMinComponent(Vector2 v)
    {
        // 수평 진동 방지
        if (Mathf.Abs(v.y) < minAbsY)
        {
            float sign = v.y == 0f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(v.y);
            v.y = sign * minAbsY;
        }

        return v;
    }
}