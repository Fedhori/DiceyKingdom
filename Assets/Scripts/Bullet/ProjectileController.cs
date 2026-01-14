using Data;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ProjectileController : MonoBehaviour
{
    [SerializeField] Collider2D blockCollider;
    [SerializeField] Collider2D wallCollider;
    [SerializeField] Rigidbody2D rb;
    
    ItemInstance item;
    Vector2 direction;
    int pierceRemaining;
    int wallBounceRemaining;
    bool hasExploded;

    public void Initialize(ItemInstance inst, Vector2 dir)
    {
        if (!enabled)
            return;

        item = inst;
        direction = dir.normalized;
        pierceRemaining = 0;
        hasExploded = false;

        ApplyPierceCount();
        ApplyWallBounceCount();
        ApplyStats();
        ApplyHitBehavior();
    }

    void Update()
    {
        if (item == null)
            return;

        UpdateHoming();
    }

    void ApplyStats()
    {
        if (rb == null || item == null)
            return;

        rb.linearVelocity = direction * item.WorldProjectileSpeed;

        float s = item.WorldProjectileSize;
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            s *= Mathf.Max(0.1f, (float)player.ProjectileSizeMultiplier);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other, blockCollider, isTrigger: true);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
            return;

        HandleHit(collision.collider, collision.otherCollider, isTrigger: false);
    }

    void HandleHit(Collider2D other, Collider2D selfCollider, bool isTrigger)
    {
        if (!enabled || item == null || other == null || selfCollider == null)
            return;

        if (isTrigger)
        {
            if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
                return;

            if (item.ProjectileExplosionRadius > 0f)
            {
                Explode(other);
                return;
            }

            var result = ApplyDamage(other);
            if (result.AppliedDamage > 0)
                HandlePierce();
            return;
        }

        if (selfCollider == wallCollider)
        {
            if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
                return;

            wallBounceRemaining--;
            if (wallBounceRemaining <= 0)
                 wallCollider.enabled = false;
            return;
        }

        if (selfCollider == blockCollider)
        {
            if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
            {
                if (item.ProjectileExplosionRadius > 0f)
                    Explode(other);
                else
                    ApplyDamage(other);
            }
            else
            {
                if (item.ProjectileExplosionRadius > 0f)
                {
                    Explode(other);
                    return;
                }

                var result = ApplyDamage(other);
                if (result.AppliedDamage > 0)
                    HandlePierce();
            }
        }
    }
    
    DamageResult ApplyDamage(Collider2D other)
    {
        if (item == null || other == null)
            return new DamageResult(0, 0, false, false);

        var block = other.GetComponent<BlockController>();
        if (block == null || block.Instance == null)
            return new DamageResult(0, 0, false, false);

        var damageManager = DamageManager.Instance;
        if (damageManager == null)
            return new DamageResult(0, 0, false, false);

        var context = new DamageContext(
            block,
            baseDamage: null,
            sourceItem: item,
            sourceType: DamageSourceType.Projectile,
            hitPosition: transform.position,
            allowOverflow: true,
            applyStatusFromItem: true,
            sourceOwner: this);
        return damageManager.ApplyDamage(context);
    }

    void Explode(Collider2D other)
    {
        if (hasExploded)
            return;

        hasExploded = true;

        var damageManager = DamageManager.Instance;
        if (damageManager != null)
        {
            Vector2 pos = transform.position;
            damageManager.ApplyAreaDamage(
                pos,
                item.ProjectileExplosionRadius,
                item,
                DamageSourceType.Projectile,
                damageScale: 1f,
                sourceOwner: this);
        }

        ParticleManager.Instance?.PlayExplosion(transform.position, item.ProjectileExplosionRadius);
        Destroy(gameObject);
    }

    void ApplyPierceCount()
    {
        if (item == null)
            return;

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
            return;

        int bonus = ItemManager.Instance != null ? ItemManager.Instance.GetPierceBonus() : 0;
        pierceRemaining = item.Pierce + bonus + 1;
    }

    void ApplyWallBounceCount()
    {
        wallBounceRemaining = 0;

        if (item == null)
            return;

        if (item.ProjectileHitBehavior != ProjectileHitBehavior.Normal)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        wallBounceRemaining = player.WallBounceCount;
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
        pierceRemaining--;
        if (pierceRemaining <= 0)
            Destroy(gameObject);
    }

    void ApplyHitBehavior()
    {
         switch (item.ProjectileHitBehavior)
        {
            case ProjectileHitBehavior.Normal:
            {
                blockCollider.isTrigger = true;
                wallCollider.enabled = wallBounceRemaining > 0;
                break;
            }
            case ProjectileHitBehavior.Bounce:
            {
                blockCollider.isTrigger = false;
                wallCollider.enabled = true;
                break;
            }
            default:
            {
                Debug.LogError("Unhandled ProjectileHitBehavior: " + item.ProjectileHitBehavior);
                break;
            }
        }
    }
}
