using System.Collections.Generic;
using UnityEngine;

public sealed class BulletFactory : MonoBehaviour
{
    public static BulletFactory Instance { get; private set; }

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private List<ProjectilePrefabEntry> projectilePrefabs = new();
    [SerializeField] private Transform bulletParent;

    readonly Dictionary<string, GameObject> prefabMap = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildPrefabMap();
    }

    void BuildPrefabMap()
    {
        prefabMap.Clear();

        if (projectilePrefabs == null)
            return;

        for (int i = 0; i < projectilePrefabs.Count; i++)
        {
            var entry = projectilePrefabs[i];
            if (string.IsNullOrEmpty(entry.key) || entry.prefab == null)
                continue;

            if (prefabMap.ContainsKey(entry.key))
            {
                Debug.LogWarning($"[BulletFactory] Duplicate projectile key '{entry.key}'.");
                continue;
            }

            prefabMap.Add(entry.key, entry.prefab);
        }
    }

    GameObject ResolvePrefab(ItemInstance item)
    {
        if (item != null && !string.IsNullOrEmpty(item.ProjectileKey))
        {
            if (prefabMap.TryGetValue(item.ProjectileKey, out var mapped) && mapped != null)
                return mapped;
        }

        return bulletPrefab;
    }

    public void SpawnBullet(Vector3 position, Vector2 direction, ItemInstance item)
    {
        var prefab = ResolvePrefab(item);
        if (prefab == null)
        {
            Debug.LogError("[BulletFactory] bullet prefab not assigned");
            return;
        }

        var go = Instantiate(prefab, position, Quaternion.identity, bulletParent);
        var ctrl = go.GetComponent<BulletController>();
        if (ctrl == null)
        {
            Debug.LogError("[BulletFactory] BulletController missing on prefab");
            Destroy(go);
            return;
        }

        ctrl.Initialize(item, direction);
    }

    [System.Serializable]
    public struct ProjectilePrefabEntry
    {
        public string key;
        public GameObject prefab;
    }
}
