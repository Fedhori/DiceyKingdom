using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform playArea;
    [SerializeField] private float startYOffset = 64f;

    PlayerInstance player;
    SpriteRenderer playAreaRenderer;
    Vector2 minBounds;
    Vector2 maxBounds;

    void Awake()
    {
        if (playArea != null)
            playAreaRenderer = playArea.GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        player = PlayerManager.Instance?.Current;
        CacheBounds();
        ResetPosition();
    }

    void Update()
    {
        player ??= PlayerManager.Instance?.Current;
        if (player == null)
            return;

        CacheBounds();

        float dir = GetMoveInput();
        if (Mathf.Approximately(dir, 0f))
            return;

        float speed = player.WorldMoveSpeed;
        Vector3 pos = transform.position;
        pos.x += dir * speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        transform.position = pos;
    }

    void CacheBounds()
    {
        if (playAreaRenderer != null)
        {
            var b = playAreaRenderer.bounds;
            minBounds = b.min;
            maxBounds = b.max;
        }
        else if (playArea != null)
        {
            var p = playArea.position;
            minBounds = new Vector2(p.x - 500f, p.y - 500f);
            maxBounds = new Vector2(p.x + 500f, p.y + 500f);
        }
    }

    void ResetPosition()
    {
        float startX = 0f;
        float startY = minBounds.y + startYOffset;
        transform.position = new Vector3(startX, startY, transform.position.z);
    }

    float GetMoveInput()
    {
        float dir = 0f;

        // Keyboard/controller (A/D, MoveX)
        if (InputManager.Instance != null)
            dir += InputManager.Instance.GetMoveX();

        // Pointer/touch: 좌/우 반 화면(PlayArea) 클릭/홀드 시 좌우 이동
        var pointer = Pointer.current;
        if (pointer != null && pointer.press != null && pointer.press.isPressed)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector2 screenPos = pointer.position.ReadValue();
                Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
                if (wp.x >= minBounds.x && wp.x <= maxBounds.x && wp.y >= minBounds.y && wp.y <= maxBounds.y)
                {
                    float centerX = (minBounds.x + maxBounds.x) * 0.5f;
                    dir += wp.x < centerX ? -1f : 1f;
                }
            }
        }

        return Mathf.Clamp(dir, -1f, 1f);
    }
}
