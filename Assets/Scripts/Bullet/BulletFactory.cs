using UnityEngine;

public sealed class BulletFactory : MonoBehaviour
{
    public static BulletFactory Instance { get; private set; }

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletParent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SpawnBullet(Vector3 position, Vector2 direction, ItemInstance item)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[BulletFactory] bulletPrefab not assigned");
            return;
        }

        var go = Instantiate(bulletPrefab, position, Quaternion.identity, bulletParent);
        var ctrl = go.GetComponent<BulletController>();
        if (ctrl == null)
        {
            Debug.LogError("[BulletFactory] BulletController missing on prefab");
            Destroy(go);
            return;
        }

        ctrl.Initialize(item, direction);
    }
}
