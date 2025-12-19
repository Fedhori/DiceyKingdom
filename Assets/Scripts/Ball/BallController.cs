using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BallController : MonoBehaviour
{
    public BallInstance Instance { get; private set; }

    bool initialized;

    [SerializeField] private SpriteRenderer ballSprite;
    [SerializeField] private Rigidbody2D ballRigidbody2D;

    public void FixedUpdate()
    {
        if (Instance == null)
            return;

        if (!Mathf.Approximately(Instance.PendingSpeedFactor, 1f))
        {
            var v = ballRigidbody2D.linearVelocity;
            float currentSpeed = v.magnitude;
            float targetSpeed = Mathf.Max(currentSpeed, Instance.PendingSpeedFactor);

            // 속도가 0이 아니고, 변경 여지가 있을 때만 보정
            if (currentSpeed > 0f && !Mathf.Approximately(currentSpeed, targetSpeed))
            {
                Vector2 dir = v / currentSpeed;      // 정규화 방향
                ballRigidbody2D.linearVelocity = dir * targetSpeed;
            }

            Instance.PendingSpeedFactor = 1f;
        }

        if (!Mathf.Approximately(Instance.PendingSizeFactor, 1f))
        {
            transform.localScale = new Vector2(Instance.PendingSizeFactor, Instance.PendingSizeFactor);
            Instance.PendingSizeFactor = 1f; // <- 원래 PendingSpeedFactor 리셋하던 버그 수정
        }
    }

    public void Initialize(BallRarity rarity, float rarityGrowth)
    {
        if (initialized)
        {
            Debug.LogWarning($"[BallController] Already initialized on {name}.");
            return;
        }

        Instance = new BallInstance(rarity, rarityGrowth);

        if (ballSprite != null)
            ballSprite.color = Instance.RarityColor;

        initialized = true;

        // Initialize가 성공적으로 끝났으니, 이제 활성 볼로 등록
        BallManager.Instance?.RegisterBall(this);
    }

    public void OnDisable()
    {
        // Instance가 null일 수 있으니 가드
        if (Instance != null)
        {
            PinEffectManager.Instance?.OnBallDestroyed(Instance);
        }

        // 아직 초기화되지 않은 상태에서 disable될 수도 있으므로 가드
        if (initialized)
        {
            BallManager.Instance?.UnregisterBall(this);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!initialized || Instance == null)
        {
            Debug.LogWarning($"[BallController] Collision received before initialization on {name}.");
            return;
        }

        var otherCollider = collision.collider;

        if (otherCollider == null)
            return;

        if (otherCollider.TryGetComponent<PinController>(out var pin))
        {
            HandleBallPinCollision(pin, collision);
            return;
        }

        if (otherCollider.TryGetComponent<BallController>(out var otherBall))
        {
            HandleBallBallCollision(otherBall, collision);
        } 
    }

    void HandleBallPinCollision(PinController pin, Collision2D collision)
    {
        if (pin == null || pin.Instance == null)
            return;

        StageManager.Instance?.ResetNoScoreTimer();
        pin.Instance.OnHitByBall(Instance, pin.transform.position);
        Instance.OnHitPin(pin.Instance, transform.position);
        pin.PlayHitEffect();
    }

    void HandleBallBallCollision(BallController otherBall, Collision2D collision)
    {
        if (otherBall == null || otherBall.Instance == null)
            return;

        if (GetInstanceID() >= otherBall.GetInstanceID())
            return;

        Instance.OnHitBall(otherBall.Instance, transform.position);
        otherBall.Instance.OnHitBall(Instance, otherBall.transform.position);
    }
}
