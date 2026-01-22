using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] private Transform playArea;
    [SerializeField] private float startYOffset = 64f;

    PlayerInstance player;
    SpriteRenderer playAreaRenderer;
    Vector2 minBounds;
    Vector2 maxBounds;
    public bool IsMoveInputActive { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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
        {
            IsMoveInputActive = false;
            return;
        }

        var stageManager = StageManager.Instance;
        if (stageManager == null || stageManager.CurrentPhase != StagePhase.Play)
        {
            IsMoveInputActive = false;
            return;
        }

        CacheBounds();

        float dir = GetMoveInput();
        IsMoveInputActive = !Mathf.Approximately(dir, 0f);
        if (!IsMoveInputActive)
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

    public void ResetToStart()
    {
        CacheBounds();
        ResetPosition();
    }

    float GetMoveInput()
    {
        float dir = 0f;

        // Keyboard/controller (A/D, MoveX)
        if (InputManager.Instance != null)
            dir += InputManager.Instance.GetMoveX();

        // Touch/pointer: 마지막으로 누른 터치 우선
        var cam = Camera.main;
        if (cam != null)
            dir += GetTouchMoveInput(cam);

        return Mathf.Clamp(dir, -1f, 1f);
    }

    float GetTouchMoveInput(Camera cam)
    {
        float dir = 0f;

        var screen = Touchscreen.current;
        if (screen != null)
        {
            TouchControl latest = null;
            double latestStart = double.MinValue;

            foreach (var touch in screen.touches)
            {
                if (touch == null || !touch.press.isPressed)
                    continue;

                Vector2 screenPos = touch.position.ReadValue();
                if (!IsInPlayArea(screenPos, cam))
                    continue;

                double startTime = touch.startTime.ReadValue();
                if (startTime > latestStart)
                {
                    latestStart = startTime;
                    latest = touch;
                }
            }

            if (latest != null)
                return GetDirectionFromScreenPos(latest.position.ReadValue(), cam);
        }

        var pointer = Pointer.current;
        if (pointer != null && pointer.press != null && pointer.press.isPressed)
        {
            Vector2 screenPos = pointer.position.ReadValue();
            if (IsInPlayArea(screenPos, cam))
                dir = GetDirectionFromScreenPos(screenPos, cam);
        }

        return dir;
    }

    bool IsInPlayArea(Vector2 screenPos, Camera cam)
    {
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        return wp.x >= minBounds.x && wp.x <= maxBounds.x && wp.y >= minBounds.y && wp.y <= maxBounds.y;
    }

    float GetDirectionFromScreenPos(Vector2 screenPos, Camera cam)
    {
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        float centerX = (minBounds.x + maxBounds.x) * 0.5f;
        return wp.x < centerX ? -1f : 1f;
    }
}
