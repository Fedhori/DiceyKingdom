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

        if (otherCollider.TryGetComponent<NailController>(out var nail))
        {
            HandleBallNailCollision(nail, collision);
            return;
        }

        if (otherCollider.TryGetComponent<BallController>(out var otherBall))
        {
            HandleBallBallCollision(otherBall, collision);
        }
    }

    void HandleBallNailCollision(NailController nail, Collision2D collision)
    {
        if (nail == null || nail.Instance == null)
            return;

        Instance.OnHitNail(nail.Instance, transform.position);
        nail.Instance.OnHitByBall(Instance);
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
