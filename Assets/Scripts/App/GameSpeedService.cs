

using UnityEngine;
using UnityEngine.UI;




namespace Game.App
{
    public class GameSpeedService : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button pauseToggleButton;    
        [SerializeField] private Image pauseToggleIcon;       
        [SerializeField] private Sprite runningIconSprite;    
        [SerializeField] private Sprite pausedIconSprite;     

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
                {
                    
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

        
        
        public void TogglePauseOrNormalSpeed()
        {
            if (ForcePaused)
                return;

            if (IsPaused)
            {
                
                GameSpeed = 1f;
            }
            else
            {
                
                IsPaused = true;
            }
        }

        
        private void UpdatePauseButtonVisual()
        {
            if (pauseToggleButton != null)
            {
                
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
            else 
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


}
