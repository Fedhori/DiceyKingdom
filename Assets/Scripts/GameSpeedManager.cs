// Assets/Scripts/Systems/GameSpeedManager.cs

using UnityEngine;
using UnityEngine.UI;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Button pauseToggleButton;    // 클릭하는 버튼
    [SerializeField] private Image pauseToggleIcon;       // 아이콘을 바꿀 Image
    [SerializeField] private Sprite runningIconSprite;    // "정지 아님" 상태 아이콘
    [SerializeField] private Sprite pausedIconSprite;     // "정지" 상태 아이콘

    private bool forcePaused;
    public bool ForcePaused
    {
        get => forcePaused;
        set
        {
            forcePaused = value;
            IsPaused = forcePaused;   // 강제 정지 시 항상 멈춘 상태 유지
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
                // 강제 정지 중이면 속도 변경 요청은 무시하고 항상 정지
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    // UI 버튼에서 호출할 메서드
    // 정지 <-> 1배속 토글 (2배, 4배는 개발자만 별도 경로로 사용)
    public void TogglePauseOrNormalSpeed()
    {
        if (ForcePaused)
            return;

        if (IsPaused)
        {
            // 정지 상태였다면 1배속으로 재생
            GameSpeed = 1f;
        }
        else
        {
            // 재생 상태였다면 정지
            IsPaused = true;
        }
    }

    // 버튼 상태/아이콘 갱신
    private void UpdatePauseButtonVisual()
    {
        if (pauseToggleButton != null)
        {
            // ForcePaused면 버튼은 비활성화(누를 수 없음)만 하고, 숨기지는 않음
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

    private void OnDisable() => ResetTime();
    private void OnApplicationQuit() => ResetTime();

    private void ResetTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = BaseFixedDeltaTime;
    }
}
