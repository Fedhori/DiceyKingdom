using UnityEngine;

public class OscillatingBlock : MonoBehaviour
{
    [SerializeField] float coverRange = 1000f;
    [SerializeField] bool oscillateHorizontal = true;
    [SerializeField] float speed = 200f;
    [SerializeField] bool randomizeStart = true;

    Vector2 centerPos;
    BoxCollider2D col;
    Rigidbody2D rb;

    float travelHalfRange;
    float phase;
    System.Random Rng => GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        centerPos = transform.position;

        RecalculateRange();

        if (randomizeStart)
        {
            SetRandomStart();
        }
    }

    void RecalculateRange()
    {
        float colliderLen = oscillateHorizontal ? col.bounds.size.x : col.bounds.size.y;
        travelHalfRange = Mathf.Max(0f, coverRange * 0.5f - colliderLen * 0.5f);
    }

    void SetRandomStart()
    {
        if (travelHalfRange <= 0f)
        {
            phase = 0f;
            return;
        }

        float startOffset = -travelHalfRange + (float)Rng.NextDouble() * (travelHalfRange * 2f);

        float u = startOffset / travelHalfRange;
        float a = Mathf.Asin(u);
        phase = (Rng.NextDouble() < 0.5d) ? a : (Mathf.PI - a);

        Vector2 axis = oscillateHorizontal ? Vector2.right : Vector2.up;
        Vector2 pos = centerPos + axis * startOffset;

        if (rb != null)
            rb.position = pos;
        else
            transform.position = pos;
    }

    void FixedUpdate()
    {
        if (travelHalfRange <= 0f)
            return;

        float omega = speed / travelHalfRange; // max 속도 = speed
        float offset = Mathf.Sin(omega * Time.fixedTime + phase) * travelHalfRange;

        Vector2 axis = oscillateHorizontal ? Vector2.right : Vector2.up;
        Vector2 pos = centerPos + axis * offset;

        if (rb != null)
            rb.MovePosition(pos);
        else
            transform.position = pos;
    }
}
