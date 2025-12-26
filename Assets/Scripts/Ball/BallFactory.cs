using UnityEngine;

public sealed class BallFactory : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;
    [SerializeField] Transform ballParent;
    public static BallFactory Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnBall(BallRarity rarity, Vector2 spawnPosition)
    {
        if (FlowManager.Instance.CurrentPhase != FlowPhase.Play)
        {
            Debug.LogError($"Invalid Phase Type. Phase: {FlowManager.Instance.CurrentPhase}");
            return;
        }

        var ball = Instantiate(ballPrefab, ballParent, false);
        ball.transform.localPosition = spawnPosition;
        ball.transform.localRotation = Quaternion.identity;

        var controller = ball.GetComponent<BallController>();
        float growth = PlayerManager.Instance?.Current?.RarityGrowth ?? 1f;
        controller.Initialize(rarity, growth);

        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = BallManager.Instance != null ? BallManager.Instance.LaunchDirection : Vector2.up;
            if (dir == Vector2.zero)
                dir = Vector2.up;

            rb.linearVelocity = dir.normalized * GameConfig.BallSpeed;
        }
    }
}