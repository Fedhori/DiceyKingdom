using Data;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BlockController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject freezeMask;

    public BlockInstance Instance { get; private set; }

    public void Initialize(BlockInstance instance)
    {
        Instance = instance;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

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
        UpdateFreezeMask();

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

    public void ApplyDamage(int amount, Vector2? position)
    {
        ApplyDamage(amount, position, null);
    }

    public void ApplyDamage(int amount, Vector2? position, ItemInstance sourceItem)
    {
        if (Instance == null)
            return;

        int currentHp = Instance.Hp;
        Instance.ApplyDamage(amount);
        RefreshHpText();

        var pos = position ?? (Vector2)transform.position;
        DamageTextManager.Instance?.ShowDamageText(amount, 0, pos);

        ApplyStatusFromItem(sourceItem);

        if (Instance.IsDead)
        {
            AudioManager.Instance.Play("Pop");
            BlockManager.Instance?.HandleBlockDestroyed(this);
            Destroy(gameObject);
            int overflow = Mathf.Max(0, amount - currentHp);
            if (overflow > 0)
                ItemManager.Instance?.TriggerOverflowDamage(overflow);
        }
            
    }

    void ApplyStatusFromItem(ItemInstance sourceItem)
    {
        if (sourceItem == null)
            return;

        if (sourceItem.StatusType == BlockStatusType.Unknown)
            return;

        float duration = sourceItem.StatusDuration;
        if (duration <= 0f)
            return;

        ApplyStatus(sourceItem.StatusType, duration);
    }

    public void ApplyStatus(BlockStatusType type, float durationSeconds)
    {
        if (Instance == null)
            return;

        if (Instance.TryApplyStatus(type, durationSeconds))
        {
            ItemManager.Instance?.TriggerAll(ItemTriggerType.OnBlockStatusApplied);
            return;
        }

        Instance.TryUpdateStatusDuration(type, durationSeconds);
    }

    void RefreshHpText()
    {
        if (hpText == null || Instance == null)
            return;

        hpText.text = Instance.Hp.ToString();
    }

    void UpdateFreezeMask()
    {
        if (freezeMask == null || Instance == null)
            return;

        freezeMask.SetActive(Instance.HasStatus(BlockStatusType.Freeze));
    }
}
