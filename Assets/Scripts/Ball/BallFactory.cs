using System;
using UnityEngine;

public sealed class BallFactory : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;
    public static BallFactory Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    public BallController SpawnBall(string ballId, Vector2 position)
    {
        var obj = Instantiate(ballPrefab, position, Quaternion.identity);
        var controller = obj.GetComponent<BallController>();
        controller.Initialize(ballId);
        return controller;
    }
}