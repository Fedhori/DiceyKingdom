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

        float speedMultiplier = Instance.HasStatus(BlockStatusType.Freeze) ? 0.7f : 1f;
        float dy = GameConfig.BlockFallSpeed * speedMultiplier * delta;
        if (dy <= 0f)
            return;

        transform.position += new Vector3(0f, -dy, 0f);
    }

    public void ApplyDamage(int amount, Vector2? position)
    {
        if (Instance == null)
            return;

        int currentHp = Instance.Hp;
        Instance.ApplyDamage(amount);
        RefreshHpText();

        var pos = position ?? (Vector2)transform.position;
        DamageTextManager.Instance?.ShowDamageText(amount, 0, pos);

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

    public void ApplyStatus(BlockStatusType type, float durationSeconds)
    {
        if (Instance == null)
            return;

        if (Instance.TryApplyStatus(type, durationSeconds))
        {
            UpdateFreezeMask();
            return;
        }

        Instance.TryUpdateStatusDuration(type, durationSeconds);
        UpdateFreezeMask();
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
