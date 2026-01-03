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
        pierceRemaining = 0;
        lifeTimer = 0f;

        if (hitCollider != null && item != null)
            hitCollider.isTrigger = item.ProjectileHitBehavior != ProjectileHitBehavior.Bounce;

        ApplyPierceCount();
        ApplyStats();
    }

    void Update()
    {
        if (item == null)
            return;

        UpdateHoming();

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
            dmg = Mathf.Max(1, Mathf.FloorToInt((float)(item.Damage * player.DamageMultiplier)));

        block.ApplyDamage(dmg, transform.position);
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

    void ApplyPierceCount()
    {
        if (item == null)
            return;

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
        {
            pierceRemaining = 0;
            return;
        }

        int bonus = ItemManager.Instance != null ? ItemManager.Instance.GetPierceBouns() : 0;

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Pierce)
        {
            if (item.MaxPierces < 0)
            {
                pierceRemaining = -1;
                return;
            }

            pierceRemaining = item.MaxPierces + bonus;
            return;
        }

        pierceRemaining = bonus;
    }

    void UpdateHoming()
    {
        if (item == null || rb == null)
            return;

        if (item.ProjectileHomingTurnRate <= 0f)
            return;

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
            return;

        var blockManager = BlockManager.Instance;
        if (blockManager == null)
            return;

        var target = blockManager.GetLowestBlock(transform.position);
        if (target == null)
            return;

        Vector3 toTarget = target.transform.position - transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Vector3 current = new Vector3(direction.x, direction.y, 0f);
        if (current.sqrMagnitude <= 0.0001f)
            current = toTarget.normalized;

        float maxRadians = item.ProjectileHomingTurnRate * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 next = Vector3.RotateTowards(current, toTarget.normalized, maxRadians, 0f);
        direction = new Vector2(next.x, next.y).normalized;
        rb.linearVelocity = direction * item.WorldProjectileSpeed;
    }

    void HandlePierce()
    {
        if (pierceRemaining < 0)
            return;

        if (pierceRemaining <= 0)
        {
            Destroy(gameObject);
            return;
        }

        pierceRemaining--;
    }
}
