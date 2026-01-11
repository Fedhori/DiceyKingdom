using Data;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BlockController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Collider2D hitCollider;

    public BlockInstance Instance { get; private set; }

    Color baseColor = Color.white;
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

        Instance.TickStatuses(delta);
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

        int currentHp = Instance.Hp;
        Instance.ApplyDamage(damage);
        RefreshHpText();

        bool statusApplied = false;
        if (context.ApplyStatusFromItem)
            statusApplied = ApplyStatusFromItem(context.SourceItem);

        bool isDead = Instance.IsDead;
        if (isDead)
            MarkPendingDestroy();
        int overflow = isDead ? Mathf.Max(0, damage - currentHp) : 0;

        return new DamageResult(damage, overflow, isDead, statusApplied);
    }

    int ResolveDamage(DamageContext context)
    {
        if (context.BaseDamage.HasValue)
            return Mathf.Max(0, context.BaseDamage.Value);

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return 0;

        float itemMultiplier = 1f;
        if (context.SourceItem != null)
            itemMultiplier = context.SourceItem.DamageMultiplier;

        if (context.SourceType == DamageSourceType.Projectile)
        {
            if (context.SourceItem != null
                && context.SourceItem.StatusDamageMultiplier > 0f
                && Instance.Statuses.Count > 0)
            {
                itemMultiplier *= context.SourceItem.StatusDamageMultiplier;
            }

            itemMultiplier *= Mathf.Max(0f, (float)player.ProjectileDamageMultiplier);
        }

        float raw = itemMultiplier * (float)player.Power;
        return Mathf.Max(1, Mathf.FloorToInt(raw));
    }

    bool ApplyStatusFromItem(ItemInstance sourceItem)
    {
        if (sourceItem == null)
            return false;

        if (sourceItem.StatusType == BlockStatusType.Unknown)
            return false;

        float duration = sourceItem.StatusDuration;
        if (duration <= 0f)
            return false;

        return ApplyStatus(sourceItem.StatusType, duration);
    }

    public bool ApplyStatus(BlockStatusType type, float durationSeconds)
    {
        if (Instance == null)
            return false;

        if (Instance.TryApplyStatus(type, durationSeconds))
            return true;

        Instance.TryUpdateStatusDuration(type, durationSeconds);
        return false;
    }

    void RefreshHpText()
    {
        if (hpText == null || Instance == null)
            return;

        hpText.text = Instance.Hp.ToString();
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

        Color color = baseColor;

        if (Instance.HasStatus(BlockStatusType.Freeze))
            color = Color.Lerp(color, Colors.FreezeTint, Colors.FreezeAlpha);

        if (hitFlashTimer > 0f && hitFlashDuration > 0f)
        {
            float t = hitFlashTimer / hitFlashDuration;
            color = Color.Lerp(color, Colors.DamageFlash, t);
        }

        spriteRenderer.color = color;
    }

    void CacheBaseColor()
    {
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
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
