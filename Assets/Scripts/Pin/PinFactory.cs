using System;
using UnityEngine;

public sealed class PinFactory : MonoBehaviour
{
    [SerializeField] GameObject pinPrefab;
    public static PinFactory Instance;

    private void Awake()
    {
        Instance = this;
    }

    public PinController SpawnPin(string pinId, Vector2 position)
    {
        var obj = Instantiate(pinPrefab, position, Quaternion.identity);
        var controller = obj.GetComponent<PinController>();
        controller.Initialize(pinId);
        return controller;
    }
}