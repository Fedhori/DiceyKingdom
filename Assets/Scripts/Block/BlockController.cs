using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BlockController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text hpText;

    public BlockInstance Instance { get; private set; }

    public void Initialize(BlockInstance instance)
    {
        Instance = instance;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        RefreshHpText();
    }

    public void SetGridPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    public void ApplyDamage(int amount, Vector2? position)
    {
        if (Instance == null)
            return;

        Instance.ApplyDamage(amount);
        RefreshHpText();

        var pos = position ?? (Vector2)transform.position;
        DamageTextManager.Instance?.ShowDamageText(amount, 0, pos);

        if (Instance.IsDead)
        {
            AudioManager.Instance.Play("Pop");
            BlockManager.Instance?.HandleBlockDestroyed(this);
            Destroy(gameObject);
        }
            
    }

    void RefreshHpText()
    {
        if (hpText == null || Instance == null)
            return;

        hpText.text = Instance.Hp.ToString();
    }
}
