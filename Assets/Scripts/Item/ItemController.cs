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
        if (item == null || BulletFactory.Instance == null || firePoint == null)
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
        BulletFactory.Instance.SpawnBullet(pos, dir, item);
    }
}
