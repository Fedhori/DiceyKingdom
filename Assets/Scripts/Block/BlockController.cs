using Data;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BlockController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer hpBarRenderer;
    [SerializeField] private SpriteRenderer statusMaskRenderer;
    [SerializeField] private SpriteRenderer hitMaskRenderer;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Collider2D hitCollider;

    public BlockInstance Instance { get; private set; }

    Color baseColor = Color.white;
    Vector2 hpBarBaseSize;
    float hitFlashTimer;
    bool isPendingDestroy;

    public void Initialize(BlockInstance instance)
    {
        Instance = instance;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        SetColliderEnabled(true);
        isPendingDestroy = false;

        CacheBaseColor();
        CacheHpBarSize();
        RefreshHpText();
    }

    void Update()
    {
        var stage = StageManager.Instance;
        if (stage == null || stage.CurrentPhase != StagePhase.Play)
            return;

        if (Instance == null)
            return;

        float delta = Time.deltaTime;
        if (delta <= 0f)
            return;

        UpdateHitFlash(delta);
        Instance.UpdateStatuses(delta);
        UpdateVisuals();

        float speedMultiplier = Instance.SpeedMultiplier;
        if (Instance.HasStatus(BlockStatusType.Freeze))
        {
            float freezeMultiplier = 0.7f;
            var player = PlayerManager.Instance?.Current;
            if (player != null && player.IsDryIceEnabled)
                freezeMultiplier = 0.4f;
            speedMultiplier *= freezeMultiplier;
        }
        float dy = GameConfig.BlockFallSpeed * speedMultiplier * delta;
        if (dy <= 0f)
            return;

        transform.position += new Vector3(0f, -dy, 0f);
    }

    public DamageResult ApplyDamage(DamageContext context)
    {
        if (Instance == null || context == null)
            return new DamageResult(0, false, false, 0);

        if (isPendingDestroy)
            return new DamageResult(0, true, false, 0);

        if (Instance.IsDead)
        {
            MarkPendingDestroy();
            return new DamageResult(0, true, false, 0);
        }

        int damage = ResolveDamage(context, out int criticalLevel);
        if (damage <= 0)
            return new DamageResult(0, false, false, 0);

        Instance.ApplyDamage(damage);
        RefreshHpText();

        bool statusApplied = false;
        if (context.ApplyStatusFromItem)
            statusApplied = ApplyStatusFromItem(context.SourceItem);

        bool isDead = Instance.IsDead;
        if (isDead)
            MarkPendingDestroy();

        return new DamageResult(damage, isDead, statusApplied, criticalLevel);
    }

    int ResolveDamage(DamageContext context, out int criticalLevel)
    {
        criticalLevel = 0;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return 0;

        float itemMultiplier = 1f;
        double criticalChance = player.CriticalChance;
        if (context.SourceItem != null)
        {
            itemMultiplier = context.SourceItem.DamageMultiplier;
            criticalChance *= Mathf.Max(0f, context.SourceItem.CriticalChanceMultiplier);
        }

        if (context.SourceItem != null
            && context.SourceItem.StatusDamageMultiplier > 0f
            && Instance.Statuses.Count > 0)
        {
            itemMultiplier *= context.SourceItem.StatusDamageMultiplier;
        }

        if (context.SourceType == DamageSourceType.Projectile)
            itemMultiplier *= Mathf.Max(0f, (float)player.ProjectileDamageMultiplier);

        float raw = itemMultiplier * (float)player.Power;
        raw *= Mathf.Max(0f, context.DamageScale);
        
        var rng = GameManager.Instance != null ? GameManager.Instance.Rng : null;
        criticalLevel = player.RollCriticalLevel(rng, criticalChance);
        raw *= (float)player.GetCriticalMultiplier(criticalLevel);
        
        int damage = Mathf.FloorToInt(raw);
        return context.AllowZeroDamage ? Mathf.Max(0, damage) : Mathf.Max(1, damage);
    }

    bool ApplyStatusFromItem(ItemInstance sourceItem)
    {
        if (sourceItem == null)
            return false;

        bool applied = false;
        var statusKeys = StatusUtil.Keys;
        for (int i = 0; i < statusKeys.Count; i++)
        {
            string key = statusKeys[i];
            int stack = StatusUtil.GetItemStatusValue(sourceItem, key);
            if (stack <= 0)
                continue;

            if (!StatusUtil.TryGetStatusType(key, out var type))
                continue;

            applied |= ApplyStatus(type, stack);
        }

        return applied;
    }

    public bool ApplyStatus(BlockStatusType type)
    {
        if (Instance == null)
            return false;

        return Instance.AddStatusStack(type, 1);
    }

    public bool ApplyStatus(BlockStatusType type, int stackAmount)
    {
        if (Instance == null)
            return false;

        return Instance.AddStatusStack(type, stackAmount);
    }

    void RefreshHpText()
    {
        if (Instance == null)
            return;

        if (hpText != null)
            hpText.text = Mathf.CeilToInt((float)Instance.Hp).ToString();

        UpdateHpBar();
    }

    public void PlayHitFlash()
    {
        if (hitFlashDuration <= 0f)
            return;

        hitFlashTimer = Mathf.Max(hitFlashTimer, hitFlashDuration);
    }

    void UpdateHitFlash(float deltaSeconds)
    {
        if (hitFlashTimer <= 0f || deltaSeconds <= 0f)
            return;

        hitFlashTimer = Mathf.Max(0f, hitFlashTimer - deltaSeconds);
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null || Instance == null)
            return;

        spriteRenderer.color = baseColor;

        UpdateFreezeMask();
        UpdateHitMask();
    }

    void UpdateFreezeMask()
    {
        if (statusMaskRenderer == null)
            return;

        bool isFrozen = Instance != null && Instance.HasStatus(BlockStatusType.Freeze);
        if (!isFrozen)
        {
            if (statusMaskRenderer.gameObject.activeSelf)
                statusMaskRenderer.gameObject.SetActive(false);
            return;
        }

        if (!statusMaskRenderer.gameObject.activeSelf)
            statusMaskRenderer.gameObject.SetActive(true);

        statusMaskRenderer.color = Colors.FreezeTint;
    }

    void UpdateHitMask()
    {
        if (hitMaskRenderer == null)
            return;

        bool hasHit = hitFlashTimer > 0f && hitFlashDuration > 0f;
        if (!hasHit)
        {
            if (hitMaskRenderer.gameObject.activeSelf)
                hitMaskRenderer.gameObject.SetActive(false);
            return;
        }

        if (!hitMaskRenderer.gameObject.activeSelf)
            hitMaskRenderer.gameObject.SetActive(true);

        float t = hitFlashTimer / hitFlashDuration;
        Color color = Colors.DamageFlash;
        color.a *= Mathf.Clamp01(t);
        hitMaskRenderer.color = color;
    }

    void CacheBaseColor()
    {
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    void CacheHpBarSize()
    {
        if (hpBarRenderer == null)
            return;

        hpBarBaseSize = hpBarRenderer.size;
    }

    void UpdateHpBar()
    {
        if (hpBarRenderer == null || Instance == null)
            return;

        float ratio = Instance.MaxHp > 0.0
            ? Mathf.Clamp01((float)(Instance.Hp / Instance.MaxHp))
            : 0f;

        var size = hpBarBaseSize;
        size.x *= ratio;
        hpBarRenderer.size = size;
    }

    void SetColliderEnabled(bool enabled)
    {
        if (hitCollider != null)
            hitCollider.enabled = enabled;
    }

    void MarkPendingDestroy()
    {
        if (isPendingDestroy)
            return;

        isPendingDestroy = true;
        SetColliderEnabled(false);
    }
}
