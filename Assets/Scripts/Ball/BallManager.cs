using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform generatorTransform;

    [SerializeField] private float cycle = 0.5f;
    private float currentCycle = 0f;
    private int spawnCount = 10;
    private int currentSpawnCount = 0;

    void Update()
    {
        if (currentSpawnCount >= spawnCount)
            return;
        
        if (currentCycle > cycle)
        {
            currentSpawnCount++;
            currentCycle -= cycle;
            var ball = Instantiate(ballPrefab, generatorTransform.position, Quaternion.identity);
            ball.transform.position = new Vector2(
                Random.Range(-350f, 350f),
                ball.transform.position.y);
        }

        currentCycle += Time.deltaTime;
    }
}