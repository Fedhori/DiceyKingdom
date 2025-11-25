using Data;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BallController : MonoBehaviour
{
    public BallInstance Instance { get; private set; }

    bool initialized;

    public void Initialize(string ballId)
    {
        if (initialized)
        {
            Debug.LogWarning($"[BallController] Already initialized on {name}.");
            return;
        }

        BallDto dto;
        try
        {
            dto = BallRepository.GetOrThrow(ballId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BallController] Failed to initialize ball {ballId}: {e}");
            return;
        }

        Instance = new BallInstance(dto);
        initialized = true;
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

        Instance.OnHitPin(pin.Instance, transform.position);
        pin.Instance.OnHitByBall(Instance);
        pin.PlayHitEffect();
    }

    void HandleBallBallCollision(BallController otherBall, Collision2D collision)
    {
        if (otherBall == null || otherBall.Instance == null)
            return;

        if (GetInstanceID() >= otherBall.GetInstanceID())
            return;

        Instance.OnHitBall(otherBall.Instance);
        otherBall.Instance.OnHitBall(Instance);
    }
}
