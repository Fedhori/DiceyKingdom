using UnityEngine;

public sealed class BallFactory : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;
    [SerializeField] private Transform ballParent;
    public static BallFactory Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnBall(BallRarity rarity, Vector2 position)
    {
        if (FlowManager.Instance.CurrentPhase != FlowPhase.Round)
        {
            Debug.LogError($"Invalid Phase Type. Phase: {FlowManager.Instance.CurrentPhase}");
            return;
        }

        var ball = Instantiate(ballPrefab, ballParent);
        var controller = ball.GetComponent<BallController>();
        float growth = PlayerManager.Instance?.Current?.RarityGrowth ?? 1f;
        controller.Initialize(rarity, growth);
        ball.transform.position = position;

        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Random.insideUnitCircle.normalized * 500f;
    }
}
