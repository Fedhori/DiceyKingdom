// Assets/Scripts/Systems/GameSpeedManager.cs

using UnityEngine;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }
    
    private bool forcePaused;
    public bool ForcePaused
    {
        get => forcePaused;
        set
        {
            forcePaused = value;
            IsPaused = forcePaused;
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
                isPaused = true;
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
        Instance = this;
    }

    private void Start()
    {
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = IsPaused ? 0f : GameSpeed;
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
