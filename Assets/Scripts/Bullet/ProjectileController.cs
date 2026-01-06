using Data;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ProjectileController : MonoBehaviour
{
    [SerializeField] Collider2D blockCollider;
    [SerializeField] Collider2D sideWallCollider;

    ItemInstance item;
    Rigidbody2D rb;
    Vector2 direction;
    int pierceRemaining;
    private ProjectileHitBehavior activeHitBehavior;
    private ProjectileHitBehavior baseHitBehavior;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!ValidateColliders())
            enabled = false;
    }

    public void Initialize(ItemInstance inst, Vector2 dir)
    {
        if (!enabled)
            return;

        item = inst;
        direction = dir.normalized;
        pierceRemaining = 0;
        baseHitBehavior = item?.ProjectileHitBehavior ?? ProjectileHitBehavior.Normal;
        activeHitBehavior = baseHitBehavior;

        ApplyPierceCount();
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
        transform.localScale = new Vector3(s, s, 1f);
    }

    public void SetSideWallCollisionEnabled(bool enabled)
    {
        if (!this.enabled)
            return;
        
        

        if (!enabled)
            activeHitBehavior = baseHitBehavior;
        else if (baseHitBehavior == ProjectileHitBehavior.Normal)
            activeHitBehavior = ProjectileHitBehavior.BounceSideWall;
        else
            activeHitBehavior = baseHitBehavior;

        ApplyHitBehavior();
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
            if (activeHitBehavior == ProjectileHitBehavior.Bounce)
                return;

            ApplyDamage(other);
            HandlePierce();
            return;
        }

        if (selfCollider == sideWallCollider)
        {
            if (activeHitBehavior is ProjectileHitBehavior.Bounce or ProjectileHitBehavior.BounceSideWall) 
                return;
        }

        if (selfCollider == blockCollider)
        {
            if (activeHitBehavior == ProjectileHitBehavior.Bounce)
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
            dmg = Mathf.Max(1, Mathf.FloorToInt((float)(item.DamageMultiplier * player.Power)));

        block.ApplyDamage(dmg, transform.position);
    }

    void ApplyPierceCount()
    {
        if (item == null)
            return;

        if (baseHitBehavior == ProjectileHitBehavior.Bounce)
        {
            pierceRemaining = -1;
            return;
        }

        int bonus = ItemManager.Instance != null ? ItemManager.Instance.GetPierceBonus() : 0;

        if (item.MaxPierces < 0)
        {
            pierceRemaining = -1;
            return;
        }

        pierceRemaining = item.MaxPierces + bonus;
    }

    void UpdateHoming()
    {
        if (item == null || rb == null)
            return;

        if (item.ProjectileHomingTurnRate <= 0f)
            return;

        if (baseHitBehavior == ProjectileHitBehavior.Bounce)
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

        if (pierceRemaining == 0)
        {
            Destroy(gameObject);
            return;
        }

        pierceRemaining--;
        if (pierceRemaining == 0)
            Destroy(gameObject);
    }

    void ApplyHitBehavior()
    {
        if (!ValidateColliders())
            return;

        blockCollider.enabled = true;
        blockCollider.isTrigger = activeHitBehavior != ProjectileHitBehavior.Bounce;
        sideWallCollider.enabled = activeHitBehavior != ProjectileHitBehavior.Normal;
    }

    bool ValidateColliders()
    {
        if (blockCollider != null && sideWallCollider != null)
            return true;

        Debug.LogWarning("[ProjectileController] BlockCollider/SideWallCollider not assigned.");
        return false;
    }
}
