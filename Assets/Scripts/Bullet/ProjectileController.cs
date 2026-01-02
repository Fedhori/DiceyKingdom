using Data;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class ProjectileController : MonoBehaviour
{
    ItemInstance item;
    Rigidbody2D rb;
    Collider2D hitCollider;
    Vector2 direction;
    int bounceCount;
    int pierceRemaining;
    float lifeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();
    }

    public void Initialize(ItemInstance inst, Vector2 dir)
    {
        item = inst;
        direction = dir.normalized;
        bounceCount = 0;
        pierceRemaining = item != null ? item.MaxPierces : 0;
        lifeTimer = 0f;

        if (hitCollider != null && item != null)
            hitCollider.isTrigger = item.ProjectileHitBehavior != ProjectileHitBehavior.Bounce;

        ApplyStats();
    }

    void Update()
    {
        if (item == null)
            return;

        if (item.ProjectileLifeTime > 0f)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= item.ProjectileLifeTime)
                Destroy(gameObject);
        }
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

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
        {
            ApplyDamage(other);
            HandleBounce();
            return;
        }

        ApplyDamage(other);

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Destroy)
            Destroy(gameObject);

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Pierce)
            HandlePierce();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (item == null)
            return;

        if (item.ProjectileHitBehavior != ProjectileHitBehavior.Bounce)
            return;

        ApplyDamage(collision.collider);
        HandleBounce();
    }

    void ApplyDamage(Collider2D other)
    {
        if (item == null || other == null)
            return;

        var block = other.GetComponent<BlockController>();
        if (block == null || block.Instance == null)
            return;

        var player = PlayerManager.Instance?.Current;
        int dmg = 1;
        if (player != null)
            dmg = Mathf.Max(1, Mathf.FloorToInt((float)(player.Damage * item.DamageMultiplier)));

        block.ApplyDamage(dmg);
    }

    void HandleBounce()
    {
        if (item == null)
            return;

        if (item.MaxBounces <= 0)
            return;

        bounceCount++;
        if (bounceCount >= item.MaxBounces)
            Destroy(gameObject);
    }

    void HandlePierce()
    {
        if (item == null)
            return;

        if (item.MaxPierces < 0)
            return;

        if (pierceRemaining <= 0)
        {
            Destroy(gameObject);
            return;
        }

        pierceRemaining--;
    }
}
