using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    public static OptionManager Instance { get; private set; }

    public GameObject optionOverlay;

    public Button quitGameButton;
    public Button gameRestartButton;
    public Button returnToMainMenuButton;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private SlidePanelLean optionPanelSlide;
    [SerializeField] private OverlayFader optionOverlayFader;
    bool previousForcePaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (optionOverlayFader == null && optionOverlay != null)
            optionOverlayFader = optionOverlay.GetComponent<OverlayFader>();
        ToggleOption(false);
    }

    void OnEnable()
    {
        // 씬이 로드될 때마다 OnSceneLoaded 실행
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // 이벤트 중복 등록 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateOptionButtons();
        SyncBgmSliderValue();
    }

    void Start()
    {
        InitializeBgmControls();
    }

    void HideAllOptionButtons()
    {
        gameRestartButton.gameObject.SetActive(false);
        returnToMainMenuButton.gameObject.SetActive(false);
    }

    void UpdateOptionButtons()
    {
        HideAllOptionButtons();

        quitGameButton.gameObject.SetActive(true);

        switch (SceneManager.GetActiveScene().name)
        {
            case "GameScene":
            {
                gameRestartButton.gameObject.SetActive(true);
                returnToMainMenuButton.gameObject.SetActive(true);
                break;
            }
            default:
            {
                break;
            }
        }
    }

    public void ToggleOption()
    {
        ToggleOption(!optionOverlay.activeSelf);
    }

    public void ToggleOption(bool isOpen)
    {
        if (optionOverlay.activeSelf == isOpen)
            return;

        if (isOpen)
        {
            if (optionOverlayFader != null)
                optionOverlayFader.Show();
            else
                optionOverlay.SetActive(true);
            UpdatePauseState(true);
            optionPanelSlide?.Show();
            return;
        }

        if (optionPanelSlide != null)
        {
            optionPanelSlide.Hide(() =>
            {
                if (optionOverlayFader != null)
                {
                    optionOverlayFader.Hide(UpdatePauseStateFalse);
                    return;
                }

                optionOverlay.SetActive(false);
                UpdatePauseStateFalse();
            });
            return;
        }

        if (optionOverlayFader != null)
        {
            optionOverlayFader.Hide(UpdatePauseStateFalse);
            return;
        }

        optionOverlay.SetActive(false);
        UpdatePauseStateFalse();
    }

    void InitializeBgmControls()
    {
        if (bgmSlider == null)
            return;

        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;
        bgmSlider.wholeNumbers = false;
        bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
        bgmSlider.onValueChanged.AddListener(HandleBgmSliderChanged);
        SyncBgmSliderValue();
    }

    void SyncBgmSliderValue()
    {
        if (bgmSlider == null)
            return;

        var bgm = BgmManager.Instance;
        if (bgm == null)
            return;

        bgmSlider.SetValueWithoutNotify(bgm.BaseVolume);
    }

    void HandleBgmSliderChanged(float value)
    {
        var bgm = BgmManager.Instance;
        if (bgm == null)
            return;

        bgm.SetBaseVolume(value);
    }

    public void RequestRestartGame()
    {
        ModalManager.Instance.ShowConfirmation(
            titleTable: "modal", titleKey: "modal.restart.title",
            messageTable: "modal", messageKey: "modal.restart.desc",
            onConfirm: RestartGame,
            onCancel: () => { }
        );
       
    }

    void RestartGame()
    {
        ToggleOption(false);
        GameManager.Instance?.RestartGame();
    }
    
    public void RequestReturnToMainMenu()
    {
        ModalManager.Instance.ShowConfirmation(
            titleTable: "modal", titleKey: "modal.mainmenu.title",
            messageTable: "modal", messageKey: "modal.mainmenu.desc",
            onConfirm: ReturnToMainMenu,
            onCancel: () => { }
        );
    }

    public void ReturnToMainMenu()
    {
        ToggleOption(false);
        SceneManager.LoadScene("MainMenuScene");
    }

    public void RequestQuitGame()
    {
        ModalManager.Instance.ShowConfirmation(
            titleTable: "modal", titleKey: "modal.quitgame.title",
            messageTable: "modal", messageKey: "modal.quitgame.message",
            onConfirm: QuitGame,
            onCancel: () => { }
        );
    }

    public void QuitGame()
    {
        ToggleOption(false);
        Application.Quit();
    }

    void UpdatePauseState(bool isOptionOpen)
    {
        var speedManager = GameSpeedManager.Instance;
        if (speedManager == null)
            return;

        if (isOptionOpen)
        {
            previousForcePaused = speedManager.ForcePaused;
            speedManager.ForcePaused = true;
            return;
        }
        speedManager.ForcePaused = previousForcePaused;
    }

    void UpdatePauseStateFalse()
    {
        UpdatePauseState(false);
    }
}
