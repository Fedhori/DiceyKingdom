using Data;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ProjectileController : MonoBehaviour
{
    [SerializeField] Collider2D blockCollider;
    [SerializeField] Collider2D wallCollider;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] SpriteRenderer bodyRenderer;
    
    ItemInstance item;
    Vector2 direction;
    int pierceRemaining;
    int wallBounceRemaining;
    float homingCooldownRemaining;
    float homingHitCooldownRemaining;
    BlockController lastHomingHitTarget;
    float damageScale = 1f;
    RigidbodyConstraints2D defaultConstraints;
    float lifetimeSeconds;
    float lifetimeRemaining;
    Color baseColor;
    bool hasBaseColor;
    bool isStationary;
    float stationaryStopSeconds;
    float stationaryElapsed;
    float stationaryInitialSpeed;
    bool areaDamageTick;
    float areaTickTimer;
    ContactFilter2D areaFilter;
    bool areaFilterInitialized;
    Collider2D[] areaOverlapBuffer;

    public void Initialize(ItemInstance inst, Vector2 dir, float damageScaleMultiplier = 1f)
    {
        if (!enabled)
            return;

        item = inst;
        direction = dir.normalized;
        pierceRemaining = 0;
        damageScale = Mathf.Max(0f, damageScaleMultiplier);

        ApplyPierceCount();
        ApplyWallBounceCount();
        ApplyStats();
        ApplyHitBehavior();

        homingCooldownRemaining = 0f;
        homingHitCooldownRemaining = 0f;
        lastHomingHitTarget = null;

        lifetimeSeconds = item != null ? Mathf.Max(0f, item.ProjectileLifetimeSeconds) : 0f;
        lifetimeRemaining = lifetimeSeconds;
        areaDamageTick = item != null && item.ProjectileAreaDamageTick;
        areaTickTimer = 0f;
        InitializeAreaFilter();
        EnsureAreaBuffer();
        CacheBaseColor();
        UpdateBlinkState(shouldBlink: false);
    }

    void Awake()
    {
        if (rb != null)
            defaultConstraints = rb.constraints;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        CacheBaseColor();
    }

    void Update()
    {
        if (item == null)
            return;

        if (UpdateLifetime())
            return;

        UpdateStationaryMotion();
        UpdateAreaDamageTick();

        UpdateHoming();
        UpdateRotation();
    }

    void ApplyStats()
    {
        if (rb == null || item == null)
            return;

        isStationary = item.IsStationaryProjectile && item.ProjectileHomingTurnRate <= 0f;
        stationaryStopSeconds = isStationary ? item.ProjectileStationaryStopSeconds : -1f;
        stationaryElapsed = 0f;
        stationaryInitialSpeed = item.WorldProjectileStationaryStartSpeed;

        if (isStationary)
        {
            if (stationaryStopSeconds <= 0f)
            {
                StopStationaryMotion();
            }
            else
            {
                rb.constraints = defaultConstraints;
                rb.linearVelocity = direction * stationaryInitialSpeed;
            }
        }
        else
        {
            rb.constraints = defaultConstraints;
            rb.linearVelocity = direction * item.WorldProjectileSpeed;
        }

        float s = item.WorldProjectileSize;
        float bonus = item.ProjectileSizeMultiplier;
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            bonus += (float)player.ProjectileSizeMultiplier;
        s *= Mathf.Max(0.1f, 1f + bonus);
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

        if (item.ProjectileAreaDamageTick)
            return;

        BlockController block = null;
        if (item.ProjectileHomingTurnRate > 0f && homingHitCooldownRemaining > 0f)
        {
            block = other.GetComponent<BlockController>();
            if (block != null && ReferenceEquals(block, lastHomingHitTarget))
                return;
        }

        if (isTrigger)
        {
            if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
                return;

            if (item.ProjectileExplosionRadius > 0f)
            {
                Explode();
                HandlePierce();
                return;
            }

            var result = ApplyDamage(other);
            if (result.AppliedDamage > 0)
                TriggerHomingHitCooldown(block ?? other.GetComponent<BlockController>());
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
                TriggerHomingCooldown();
                if (item.ProjectileExplosionRadius > 0f)
                    Explode();
                else
                    ApplyDamage(other);
            }
            else
            {
                if (item.ProjectileExplosionRadius > 0f)
                {
                    Explode();
                    HandlePierce();
                    return;
                }

                var result = ApplyDamage(other);
                if (result.AppliedDamage > 0)
                {
                    TriggerHomingHitCooldown(block ?? other.GetComponent<BlockController>());
                    HandlePierce();
                }
            }
        }
    }
    
    DamageResult ApplyDamage(Collider2D other)
    {
        if (item == null || other == null)
            return new DamageResult(0, false, false, 0);

        var block = other.GetComponent<BlockController>();
        if (block == null || block.Instance == null)
            return new DamageResult(0, false, false, 0);

        var damageManager = DamageManager.Instance;
        if (damageManager == null)
            return new DamageResult(0, false, false, 0);

        var context = new DamageContext(
            block,
            sourceItem: item,
            sourceType: DamageSourceType.Projectile,
            hitPosition: transform.position,
            applyStatusFromItem: true,
            sourceOwner: this,
            damageScale: damageScale);
        return damageManager.ApplyDamage(context);
    }

    void Explode()
    {
        var damageManager = DamageManager.Instance;
        if (damageManager != null)
        {
            Vector2 pos = transform.position;
            damageManager.ApplyAreaDamage(
                pos,
                item.ProjectileExplosionRadius,
                item,
                DamageSourceType.Projectile,
                damageScale: damageScale,
                sourceOwner: this);
        }

        ParticleManager.Instance?.PlayExplosion(transform.position, item.ProjectileExplosionRadius);
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

        if (homingHitCooldownRemaining > 0f)
            homingHitCooldownRemaining = Mathf.Max(0f, homingHitCooldownRemaining - Time.deltaTime);

        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce && homingCooldownRemaining > 0f)
        {
            homingCooldownRemaining = Mathf.Max(0f, homingCooldownRemaining - Time.deltaTime);
            return;
        }

        if (item.ProjectileHomingTurnRate <= 0f)
            return;

        var blockManager = BlockManager.Instance;
        if (blockManager == null)
            return;

        var target = blockManager.GetLowestBlock(transform.position);
        if (homingHitCooldownRemaining > 0f && lastHomingHitTarget != null && ReferenceEquals(target, lastHomingHitTarget))
            return;

        if (target == null)
            return;

        Vector3 toTarget = target.transform.position - transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Vector3 current;
        if (item.ProjectileHitBehavior == ProjectileHitBehavior.Bounce)
        {
            Vector2 velocity = rb.linearVelocity;
            current = velocity.sqrMagnitude > 0.0001f
                ? new Vector3(velocity.x, velocity.y, 0f)
                : new Vector3(direction.x, direction.y, 0f);
        }
        else
        {
            current = new Vector3(direction.x, direction.y, 0f);
        }

        if (current.sqrMagnitude <= 0.0001f)
            current = toTarget.normalized;

        float maxRadians = item.ProjectileHomingTurnRate * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 next = Vector3.RotateTowards(current, toTarget.normalized, maxRadians, 0f);
        direction = new Vector2(next.x, next.y).normalized;
        rb.linearVelocity = direction * item.WorldProjectileSpeed;
    }

    void TriggerHomingCooldown()
    {
        if (item == null)
            return;

        if (item.ProjectileHitBehavior != ProjectileHitBehavior.Bounce)
            return;

        if (item.ProjectileHomingTurnRate <= 0f)
            return;

        homingCooldownRemaining = Mathf.Max(0f, GameConfig.DamageTickIntervalSeconds);
    }

    void TriggerHomingHitCooldown(BlockController target)
    {
        if (item == null || target == null)
            return;

        if (item.ProjectileHomingTurnRate <= 0f)
            return;

        homingHitCooldownRemaining = Mathf.Max(0f, GameConfig.DamageTickIntervalSeconds);
        lastHomingHitTarget = target;
    }

    void UpdateRotation()
    {
        if (rb == null)
            return;

        if (item != null && item.IsStationaryProjectile)
            return;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
        rb.SetRotation(angle);
    }

    void UpdateStationaryMotion()
    {
        if (!isStationary || rb == null)
            return;

        if (stationaryStopSeconds <= 0f)
            return;

        stationaryElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(stationaryElapsed / stationaryStopSeconds);
        float speedScale = Mathf.Exp(-GameConfig.ProjectileStationaryDecayExponent * t);
        rb.linearVelocity = direction * (stationaryInitialSpeed * speedScale);

        if (t >= 1f)
            StopStationaryMotion();
    }

    void StopStationaryMotion()
    {
        if (rb == null)
            return;

        stationaryStopSeconds = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    void UpdateAreaDamageTick()
    {
        if (!areaDamageTick || blockCollider == null || item == null)
            return;

        float tickInterval = Mathf.Max(0f, GameConfig.DamageTickIntervalSeconds);
        if (tickInterval <= 0f)
            return;

        float delta = Time.deltaTime;
        if (delta <= 0f)
            return;

        areaTickTimer += delta;
        while (areaTickTimer >= tickInterval)
        {
            areaTickTimer -= tickInterval;
            ApplyAreaDamageTick(tickInterval);
        }
    }

    void ApplyAreaDamageTick(float damageScale)
    {
        if (!areaFilterInitialized || blockCollider == null || item == null)
            return;

        var damageManager = DamageManager.Instance;
        if (damageManager == null)
            return;

        EnsureAreaBuffer();
        int hitCount = blockCollider.Overlap(areaFilter, areaOverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            var hit = areaOverlapBuffer[i];
            if (hit == null)
                continue;

            var block = hit.GetComponent<BlockController>();
            if (block == null || block.Instance == null)
                continue;

            var context = new DamageContext(
                block,
                sourceItem: item,
                sourceType: DamageSourceType.Projectile,
                hitPosition: block.transform.position,
                applyStatusFromItem: true,
                sourceOwner: this,
                damageScale: damageScale,
                allowZeroDamage: true);

            damageManager.ApplyDamage(context);
        }
    }

    void InitializeAreaFilter()
    {
        if (areaFilterInitialized)
            return;

        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0)
            return;

        areaFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = 1 << blockLayer,
            useTriggers = true
        };

        areaFilterInitialized = true;
    }

    void EnsureAreaBuffer()
    {
        int size = 64;
        if (areaOverlapBuffer == null || areaOverlapBuffer.Length != size)
            areaOverlapBuffer = new Collider2D[size];
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

    bool UpdateLifetime()
    {
        if (lifetimeSeconds <= 0f)
        {
            UpdateBlinkState(shouldBlink: false);
            return false;
        }

        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            if (item != null && item.ProjectileExplosionRadius > 0f)
                Explode();
            Destroy(gameObject);
            return true;
        }

        float ratio = lifetimeRemaining / lifetimeSeconds;
        bool shouldBlink = ratio <= 0.3f;
        UpdateBlinkState(shouldBlink, 2f);
        return false;
    }

    void UpdateBlinkState(bool shouldBlink, float blinkHz = 2f)
    {
        if (bodyRenderer == null || !hasBaseColor)
            return;

        if (!shouldBlink)
        {
            bodyRenderer.color = baseColor;
            return;
        }

        float hz = Mathf.Max(0.1f, blinkHz);
        float t = Mathf.PingPong(Time.time * hz, 1f);
        float alpha = Mathf.Lerp(0.3f, 1f, t);
        var c = baseColor;
        c.a = Mathf.Clamp01(baseColor.a * alpha);
        bodyRenderer.color = c;
    }

    void CacheBaseColor()
    {
        if (bodyRenderer == null)
            return;

        baseColor = bodyRenderer.color;
        hasBaseColor = true;
    }
}
