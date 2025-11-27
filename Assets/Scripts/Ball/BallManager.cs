using UnityEngine;

public class BallManager : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;

    private float cycle = 0.1f;
    private float currentCycle = 0f;
    private int spawnCount = 20;
    private int currentSpawnCount = 0;

    void Update()
    {
        if (currentSpawnCount >= spawnCount)
            return;

        if (currentCycle > cycle)
        {
            currentSpawnCount++;
            currentCycle -= cycle;
            if (currentSpawnCount % 10 == 0)
                BallFactory.Instance.SpawnBall("ball.gold");
            else
                BallFactory.Instance.SpawnBall("ball.basic");
        }

        currentCycle += Time.deltaTime;
    }
}