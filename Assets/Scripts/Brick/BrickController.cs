using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BrickController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text hpText;

    public BrickInstance Instance { get; private set; }

    public void Initialize(BrickInstance instance)
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

    public void ApplyDamage(int amount)
    {
        if (Instance == null)
            return;

        Instance.ApplyDamage(amount);
        RefreshHpText();

        if (Instance.IsDead)
        {
            AudioManager.Instance.Play("Pop");
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
