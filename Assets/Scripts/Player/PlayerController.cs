using UnityEngine;

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

        Vector2 input = GetMoveInput();
        IsMoveInputActive = input.sqrMagnitude > 0.0001f;
        if (!IsMoveInputActive)
            return;

        float speed = player.WorldMoveSpeed;
        Vector3 pos = transform.position;
        Vector2 move = input;
        if (move.sqrMagnitude > 1f)
            move = move.normalized;

        float dt = Time.deltaTime;
        pos.x += move.x * speed * dt;
        pos.y += move.y * speed * dt;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
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

    Vector2 GetMoveInput()
    {
        if (InputManager.Instance == null)
            return Vector2.zero;

        return InputManager.Instance.GetMoveVector();
    }
}
