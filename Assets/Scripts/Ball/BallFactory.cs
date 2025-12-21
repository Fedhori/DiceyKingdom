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

    public void SpawnBall(BallRarity rarity, Vector2 localPosition)
    {
        if (FlowManager.Instance.CurrentPhase != FlowPhase.Play)
        {
            Debug.LogError($"Invalid Phase Type. Phase: {FlowManager.Instance.CurrentPhase}");
            return;
        }

        var ball = Instantiate(ballPrefab, ballParent, false);
        ball.transform.localPosition = localPosition;
        ball.transform.localRotation = Quaternion.identity;

        var controller = ball.GetComponent<BallController>();
        float growth = PlayerManager.Instance?.Current?.RarityGrowth ?? 1f;
        controller.Initialize(rarity, growth);

        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;

            rb.linearVelocity = dir.normalized * 500f;
        }
    }
}