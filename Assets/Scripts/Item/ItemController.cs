using UnityEngine;

public sealed class ItemController : MonoBehaviour
{
    [SerializeField] private Transform firePoint;

    ItemInstance item;
    float fireTimer;

    public void BindItem(ItemInstance item, Transform attachTarget)
    {
        if (item == null || attachTarget == null)
            return;

        this.item = item;
        fireTimer = 0f;
        transform.SetParent(attachTarget, worldPositionStays: true);
        transform.localPosition = Vector3.zero;
    }

    void Update()
    {
        if (item == null || ProjectileFactory.Instance == null || firePoint == null)
            return;

        float interval = 1f / Mathf.Max(0.1f, item.AttackSpeed);
        fireTimer += Time.deltaTime;
        if (fireTimer >= interval)
        {
            fireTimer -= interval;
            Fire();
        }
    }

    void Fire()
    {
        Vector3 pos = firePoint.position;
        Vector2 dir = Vector2.up;
        int count = Mathf.Max(1, item.PelletCount);
        float spread = Mathf.Max(0f, item.SpreadAngle);
        float randomAngle = Mathf.Max(0f, item.ProjectileRandomAngle);
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            randomAngle *= Mathf.Max(0f, (float)player.ProjectileRandomAngleMultiplier);

        float total = spread * (count - 1);
        float start = -total * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float angle = start + spread * i;
            Vector2 shotDir = Quaternion.Euler(0f, 0f, angle) * dir;
            if (randomAngle > 0f)
            {
                float noise = Random.Range(-randomAngle, randomAngle);
                shotDir = Quaternion.Euler(0f, 0f, noise) * shotDir;
            }
            ProjectileFactory.Instance.SpawnProjectile(pos, shotDir, item);
        }
    }
}
