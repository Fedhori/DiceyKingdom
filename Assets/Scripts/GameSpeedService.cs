// Assets/Scripts/Systems/GameSpeedService.cs

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity component that provides game speed runtime behavior.
/// </summary>
public class GameSpeedService : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button pauseToggleButton;    // ?대┃?섎뒗 踰꾪듉
    [SerializeField] private Image pauseToggleIcon;       // ?꾩씠肄섏쓣 諛붽? Image
    [SerializeField] private Sprite runningIconSprite;    // "?뺤? ?꾨떂" ?곹깭 ?꾩씠肄?
    [SerializeField] private Sprite pausedIconSprite;     // "?뺤?" ?곹깭 ?꾩씠肄?

    private bool forcePaused;
    public bool ForcePaused
    {
        get => forcePaused;
        set
        {
            forcePaused = value;
            IsPaused = forcePaused;   // 媛뺤젣 ?뺤? ????긽 硫덉텣 ?곹깭 ?좎?
        }
    }

    private bool isPaused;
    public bool IsPaused
    {
        get => isPaused;
        set
        {
            if (ForcePaused)
                isPaused = true;
            else
                isPaused = value;

            Apply();
        }
    }

    private float gameSpeed = 1.0f;
    public float GameSpeed
    {
        get => gameSpeed;
        set
        {
            if (ForcePaused)
            {
                // 媛뺤젣 ?뺤? 以묒씠硫??띾룄 蹂寃??붿껌? 臾댁떆?섍퀬 ??긽 ?뺤?
                isPaused = true;
            }
            else
            {
                gameSpeed = Mathf.Clamp(value, 1.0f, 8f);
                isPaused = false;
            }

            Apply();
        }
    }

    private const float BaseFixedDeltaTime = 0.02f;

    private void Awake()
    {
        ResetTime();
    }

    private void Start()
    {
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = IsPaused ? 0f : GameSpeed;
        UpdatePauseButtonVisual();
    }

    // UI 踰꾪듉?먯꽌 ?몄텧??硫붿꽌??
    // ?뺤? <-> 1諛곗냽 ?좉? (2諛? 4諛곕뒗 媛쒕컻?먮쭔 蹂꾨룄 寃쎈줈濡??ъ슜)
    public void TogglePauseOrNormalSpeed()
    {
        if (ForcePaused)
            return;

        if (IsPaused)
        {
            // ?뺤? ?곹깭??ㅻ㈃ 1諛곗냽?쇰줈 ?ъ깮
            GameSpeed = 1f;
        }
        else
        {
            // ?ъ깮 ?곹깭??ㅻ㈃ ?뺤?
            IsPaused = true;
        }
    }

    // 踰꾪듉 ?곹깭/?꾩씠肄?媛깆떊
    private void UpdatePauseButtonVisual()
    {
        if (pauseToggleButton != null)
        {
            // ForcePaused硫?踰꾪듉? 鍮꾪솢?깊솕(?꾨? ???놁쓬)留??섍퀬, ?④린吏???딆쓬
            pauseToggleButton.interactable = !ForcePaused;
        }

        if (pauseToggleIcon == null)
            return;

        if (IsPaused)
        {
            if (pausedIconSprite != null)
                pauseToggleIcon.sprite = pausedIconSprite;
        }
        else
        {
            if (runningIconSprite != null)
                pauseToggleIcon.sprite = runningIconSprite;
        }
    }

    public void CycleNextSpeed()
    {
        float currentSpeed = GameSpeed;

        if (Mathf.Approximately(currentSpeed, 1f))
            GameSpeed = 2f;
        else if (Mathf.Approximately(currentSpeed, 2f))
            GameSpeed = 4f;
        else // Includes 4f and any other speed, defaults to 1x
            GameSpeed = 1f;
    }

    private void OnDestroy() => ResetTime();
    private void OnApplicationQuit() => ResetTime();

    private void ResetTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = BaseFixedDeltaTime;
    }
}


