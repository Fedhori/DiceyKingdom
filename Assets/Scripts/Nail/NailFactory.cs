using System;
using UnityEngine;

public sealed class NailFactory : MonoBehaviour
{
    [SerializeField] GameObject nailPrefab;
    public static NailFactory Instance;

    private void Awake()
    {
        Instance = this;
    }

    public NailController SpawnNail(string nailId, Vector2 position)
    {
        var obj = Instantiate(nailPrefab, position, Quaternion.identity);
        var controller = obj.GetComponent<NailController>();
        controller.Initialize(nailId);
        return controller;
    }
}