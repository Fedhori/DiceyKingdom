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

        rb.linearVelocity = direction * item.WorldBulletSpeed;

        float s = item.WorldBulletSize;
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (item == null)
            return;

        var brick = other.GetComponent<BrickController>();
        if (brick != null && brick.Instance != null)
        {
            var player = PlayerManager.Instance?.Current;
            int dmg = 1;
            if (player != null)
                dmg = Mathf.Max(1, Mathf.FloorToInt((float)(player.Damage * item.DamageMultiplier)));

            brick.ApplyDamage(dmg);
            Destroy(gameObject);
            return;
        }

        // 기타 오브젝트는 무시/관통
    }
}
