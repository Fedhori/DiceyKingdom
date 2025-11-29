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

    public void SpawnBallById(string ballId)
    {
        var ball = Instantiate(ballPrefab, ballParent);
        var controller = ball.GetComponent<BallController>();
        controller.Initialize(ballId);
        ball.transform.position = new Vector2(Random.Range(-350f, 350f), 500);
    }
}