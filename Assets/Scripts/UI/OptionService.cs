using UnityEngine;
using UnityEngine.SceneManagement;
using Game.App;
using Game.Audio;
using Game.Common;
using UnityEngine.Serialization;
using UnityEngine.UI;




namespace Game.UI
{
public class OptionService : MonoBehaviour
{
    public GameObject optionOverlay;

    public Button quitGameButton;
    public Button returnToMainMenuButton;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private BgmService bgmService;
    [SerializeField] private GameSpeedService gameSpeedService;
    [SerializeField] private ModalService modalService;
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
        returnToMainMenuButton.gameObject.SetActive(false);
    }

    void UpdateOptionButtons()
    {
        HideAllOptionButtons();

        quitGameButton.gameObject.SetActive(true);
        string activeScene = SceneManager.GetActiveScene().name;
        bool showReturnToMainMenu =
            !string.Equals(activeScene, SceneIds.Bootstrap, System.StringComparison.Ordinal) &&
            !string.Equals(activeScene, SceneIds.MainMenuScene, System.StringComparison.Ordinal);
        returnToMainMenuButton.gameObject.SetActive(showReturnToMainMenu);
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

        if (bgmService == null)
            return;

        bgmSlider.SetValueWithoutNotify(bgmService.BaseVolume);
    }

    void HandleBgmSliderChanged(float value)
    {
        if (bgmService == null)
            return;

        bgmService.SetBaseVolume(value);
    }
    
    public void RequestReturnToMainMenu()
    {
        if (modalService == null)
            return;

        modalService.ShowConfirmation(
            titleTable: "modal", titleKey: "modal.mainmenu.title",
            messageTable: "modal", messageKey: "modal.mainmenu.desc",
            onConfirm: ReturnToMainMenu,
            onCancel: () => { }
        );
    }

    public void ReturnToMainMenu()
    {
        ToggleOption(false);
        SceneManager.LoadScene(SceneIds.MainMenuScene);
    }

    public void RequestQuitGame()
    {
        if (modalService == null)
            return;

        modalService.ShowConfirmation(
            titleTable: "modal", titleKey: "modal.quitgame.title",
            messageTable: "modal", messageKey: "modal.quitgame.message",
            onConfirm: QuitGame,
            onCancel: () => { }
        );
    }

    public void QuitGame()
    {
        ToggleOption(false);
        UnityEngine.Application.Quit();
    }

    void UpdatePauseState(bool isOptionOpen)
    {
        if (gameSpeedService == null)
            return;

        if (isOptionOpen)
        {
            previousForcePaused = gameSpeedService.ForcePaused;
            gameSpeedService.ForcePaused = true;
            return;
        }
        gameSpeedService.ForcePaused = previousForcePaused;
    }

    void UpdatePauseStateFalse()
    {
        UpdatePauseState(false);
    }

    void ResolveDependencies()
    {
        var appServices = GameApp.I?.App;
        if (bgmService == null)
            bgmService = appServices?.Bgm;
        if (gameSpeedService == null)
            gameSpeedService = appServices?.GameSpeed;
        if (modalService == null)
            modalService = appServices?.UI?.Modal;
    }

    public bool ValidateConfiguration(Transform appRoot)
    {
        bool valid = true;

        if (optionOverlay == null)
        {
            Debug.LogError("[OptionService] optionOverlay is not assigned.");
            valid = false;
        }
        else if (appRoot != null && !optionOverlay.transform.IsChildOf(appRoot))
        {
            Debug.LogError("[OptionService] optionOverlay must be placed under GameApp in editor.");
            valid = false;
        }

        if (quitGameButton == null)
        {
            Debug.LogError("[OptionService] quitGameButton is not assigned.");
            valid = false;
        }

        return valid;
    }
}


}
