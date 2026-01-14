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
    Vector3 hpBarBaseScale;
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
        CacheHpBarScale();
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
        UpdateVisuals();

        float speedMultiplier = 1f;
        if (Instance.HasStatus(BlockStatusType.Freeze))
        {
            float freezeMultiplier = 0.7f;
            var player = PlayerManager.Instance?.Current;
            if (player != null && player.IsDryIceEnabled)
                freezeMultiplier = 0.4f;
            speedMultiplier = freezeMultiplier;
        }
        float dy = GameConfig.BlockFallSpeed * speedMultiplier * delta;
        if (dy <= 0f)
            return;

        transform.position += new Vector3(0f, -dy, 0f);
    }

    public DamageResult ApplyDamage(DamageContext context)
    {
        if (Instance == null || context == null)
            return new DamageResult(0, 0, false, false);

        if (isPendingDestroy)
            return new DamageResult(0, 0, true, false);

        if (Instance.IsDead)
        {
            MarkPendingDestroy();
            return new DamageResult(0, 0, true, false);
        }

        int damage = ResolveDamage(context);
        if (damage <= 0)
            return new DamageResult(0, 0, false, false);

        double currentHp = Instance.Hp;
        Instance.ApplyDamage(damage);
        RefreshHpText();

        bool statusApplied = false;
        if (context.ApplyStatusFromItem)
            statusApplied = ApplyStatusFromItem(context.SourceItem);

        bool isDead = Instance.IsDead;
        if (isDead)
            MarkPendingDestroy();
        int overflow = 0;
        if (isDead)
        {
            double rawOverflow = damage - currentHp;
            if (rawOverflow > 0.0)
                overflow = Mathf.FloorToInt((float)rawOverflow);
        }

        return new DamageResult(damage, overflow, isDead, statusApplied);
    }

    int ResolveDamage(DamageContext context)
    {
        if (context.BaseDamage.HasValue)
        {
            float rawBase = Mathf.Max(0f, context.BaseDamage.Value);
            rawBase *= Mathf.Max(0f, context.DamageScale);
            int baseDamage = Mathf.FloorToInt(rawBase);
            if (baseDamage <= 0)
                return 0;

            return context.AllowZeroDamage ? Mathf.Max(0, baseDamage) : Mathf.Max(1, baseDamage);
        }

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return 0;

        float itemMultiplier = 1f;
        if (context.SourceItem != null)
            itemMultiplier = context.SourceItem.DamageMultiplier;

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
        int damage = Mathf.FloorToInt(raw);
        return context.AllowZeroDamage ? Mathf.Max(0, damage) : Mathf.Max(1, damage);
    }

    bool ApplyStatusFromItem(ItemInstance sourceItem)
    {
        if (sourceItem == null)
            return false;

        if (sourceItem.StatusType == BlockStatusType.Unknown)
            return false;

        return ApplyStatus(sourceItem.StatusType);
    }

    public bool ApplyStatus(BlockStatusType type)
    {
        if (Instance == null)
            return false;

        return Instance.TryApplyStatus(type);
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

    void CacheHpBarScale()
    {
        if (hpBarRenderer == null)
            return;

        hpBarBaseScale = hpBarRenderer.transform.localScale;
    }

    void UpdateHpBar()
    {
        if (hpBarRenderer == null || Instance == null)
            return;

        float ratio = Instance.MaxHp > 0.0
            ? Mathf.Clamp01((float)(Instance.Hp / Instance.MaxHp))
            : 0f;

        var scale = hpBarBaseScale;
        scale.x *= ratio;
        hpBarRenderer.transform.localScale = scale;
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
