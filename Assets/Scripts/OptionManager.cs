using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    public GameObject optionOverlay;

    public Button quitGameButton;
    public Button gameRestartButton;
    public Button returnToMainMenuButton;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private BgmManager bgmManager;
    [SerializeField] private GameSpeedManager gameSpeedManager;
    [SerializeField] private ModalManager modalManager;
    [SerializeField] private SlidePanelLean optionPanelSlide;
    [SerializeField] private OverlayFader optionOverlayFader;
    readonly DisposableBag subscriptions = new();
    bool previousForcePaused;

    private void Awake()
    {
        ResolveDependencies();
        if (optionOverlayFader == null && optionOverlay != null)
            optionOverlayFader = optionOverlay.GetComponent<OverlayFader>();
        ToggleOption(false);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateOptionButtons();
        SyncBgmSliderValue();
    }

    void OnEnable()
    {
        ResolveDependencies();
        subscriptions.Clear();
        subscriptions.Add(EventSubscription.Create(
            () => SceneManager.sceneLoaded += OnSceneLoaded,
            () => SceneManager.sceneLoaded -= OnSceneLoaded));
        subscriptions.Add(EventSubscription.Subscribe(bgmSlider, HandleBgmSliderChanged));
        InitializeBgmControls();
        UpdateOptionButtons();
        SyncBgmSliderValue();
    }

    void OnDisable()
    {
        subscriptions.Clear();
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
        SyncBgmSliderValue();
    }

    void SyncBgmSliderValue()
    {
        if (bgmSlider == null)
            return;

        if (bgmManager == null)
            return;

        bgmSlider.SetValueWithoutNotify(bgmManager.BaseVolume);
    }

    void HandleBgmSliderChanged(float value)
    {
        if (bgmManager == null)
            return;

        bgmManager.SetBaseVolume(value);
    }
    
    public void RequestReturnToMainMenu()
    {
        if (modalManager == null)
            return;

        modalManager.ShowConfirmation(
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
        if (modalManager == null)
            return;

        modalManager.ShowConfirmation(
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
        if (gameSpeedManager == null)
            return;

        if (isOptionOpen)
        {
            previousForcePaused = gameSpeedManager.ForcePaused;
            gameSpeedManager.ForcePaused = true;
            return;
        }
        gameSpeedManager.ForcePaused = previousForcePaused;
    }

    void UpdatePauseStateFalse()
    {
        UpdatePauseState(false);
    }

    void ResolveDependencies()
    {
        var appServices = GameApp.I?.App;
        if (bgmManager == null)
            bgmManager = appServices?.Bgm;
        if (gameSpeedManager == null)
            gameSpeedManager = appServices?.GameSpeed;
        if (modalManager == null)
            modalManager = appServices?.UI?.Modal;
    }
}
