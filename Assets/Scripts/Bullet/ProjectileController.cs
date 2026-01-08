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

    public void Initialize(ItemInstance inst, Vector2 dir)
    {
        if (!enabled)
            return;

        item = inst;
        direction = dir.normalized;
        pierceRemaining = 0;

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

            ApplyDamage(other);
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
                ApplyDamage(other);
            }
            else
            {
                ApplyDamage(other);
                HandlePierce();
            }
        }
    }

    // TODO - ApplyDamage를 ProjectileController에서 관리하면 중복이 늘어나 유지보수 피곤해짐.
    // AttackContext로 분리해서, 통일되게 동작하도록 개선 필요
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
        {
            float itemMultiplier = item.DamageMultiplier;
            if (item.StatusDamageMultiplier > 0f && block.Instance.Statuses.Count > 0)
                itemMultiplier *= item.StatusDamageMultiplier;
            itemMultiplier *= Mathf.Max(0f, (float)player.ProjectileDamageMultiplier);
            dmg = Mathf.Max(1, Mathf.FloorToInt((float)(itemMultiplier * player.Power)));
        }

        block.ApplyDamage(dmg, transform.position, item);
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
