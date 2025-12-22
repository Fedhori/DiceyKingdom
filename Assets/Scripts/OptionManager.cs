using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    public static OptionManager Instance { get; private set; }

    public GameObject optionOverlay;

    public Button quitGameButton;
    public Button gameRestartButton;
    public Button returnToMainMenuButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateOptionButtons();
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
        optionOverlay.SetActive(isOpen);
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
}