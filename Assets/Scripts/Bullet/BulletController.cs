using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BulletController : MonoBehaviour
{
    ItemInstance item;
    Rigidbody2D rb;
    Vector2 direction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(ItemInstance inst, Vector2 dir)
    {
        item = inst;
        direction = dir.normalized;
        ApplyStats();
    }

    void ApplyStats()
    {
        if (rb == null || item == null)
            return;

        rb.linearVelocity = direction * item.WorldProjectileSpeed;

        float s = item.WorldProjectileSize;
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (item == null)
            return;

        var block = other.GetComponent<BlockController>();
        if (block != null && block.Instance != null)
        {
            var player = PlayerManager.Instance?.Current;
            int dmg = 1;
            if (player != null)
                dmg = Mathf.Max(1, Mathf.FloorToInt((float)(player.Damage * item.DamageMultiplier)));

            block.ApplyDamage(dmg);
            Destroy(gameObject);
        }

        // 기타 오브젝트는 무시/관통
    }
}
