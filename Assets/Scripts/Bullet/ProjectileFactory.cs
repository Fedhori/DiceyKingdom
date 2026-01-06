using System.Collections.Generic;
using UnityEngine;

public sealed class ProjectileFactory : MonoBehaviour
{
    public static ProjectileFactory Instance { get; private set; }

    [SerializeField] private List<ProjectilePrefabEntry> projectilePrefabs = new();
    [SerializeField] private Transform parent;

    readonly Dictionary<string, GameObject> prefabMap = new();
    bool sideWallCollisionEnabled;

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
                Debug.LogWarning($"[ProjectileFactory] Duplicate projectile key '{entry.key}'.");
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

        return null;
    }

    public void SpawnProjectile(Vector3 position, Vector2 direction, ItemInstance item)
    {
        var prefab = ResolvePrefab(item);
        if (prefab == null)
        {
            Debug.LogError("[ProjectileFactory] projectile prefab not assigned");
            return;
        }

        var go = Instantiate(prefab, position, Quaternion.identity, parent);
        var ctrl = go.GetComponent<ProjectileController>();
        if (ctrl == null)
        {
            Debug.LogError("[ProjectileFactory] ProjectileController missing on prefab");
            Destroy(go);
            return;
        }

        ctrl.Initialize(item, direction);
        ctrl.SetSideWallCollisionEnabled(sideWallCollisionEnabled);
    }

    public void SetSideWallCollisionEnabled(bool enabled)
    {
        if (sideWallCollisionEnabled.Equals(enabled))
            return;

        sideWallCollisionEnabled = enabled;
    }

    public void ClearAllProjectiles()
    {
        if (parent != null)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }

            return;
        }

        var projectiles = FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
        for (int i = 0; i < projectiles.Length; i++)
        {
            var projectile = projectiles[i];
            if (projectile != null)
                Destroy(projectile.gameObject);
        }
    }

    [System.Serializable]
    public struct ProjectilePrefabEntry
    {
        public string key;
        public GameObject prefab;
    }
}
