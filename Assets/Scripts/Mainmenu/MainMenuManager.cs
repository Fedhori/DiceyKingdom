using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [SerializeField] private GameObject continueButtonRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateContinueButtonVisibility();
    }

    public void StartGame()
    {
        if (SaveService.HasValidSave())
        {
            var modal = ModalManager.Instance;
            if (modal != null)
            {
                modal.ShowConfirmation(
                    "modal",
                    "modal.newRunOverwrite.title",
                    "modal",
                    "modal.newRunOverwrite.message",
                    () =>
                    {
                        TryDeleteSavesForNewRun();
                        SceneManager.LoadScene("GameScene");
                    },
                    () => { });
                return;
            }

            Debug.LogWarning("[MainMenuManager] ModalManager not found. Starting new run without confirmation.");
            TryDeleteSavesForNewRun();
        }

        SceneManager.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        if (!SaveService.HasValidSave())
        {
            UpdateContinueButtonVisibility();
            return;
        }

        var save = SaveManager.Instance;
        if (save == null)
        {
            Debug.LogError("[MainMenuManager] SaveManager not found. Cannot continue.");
            return;
        }

        save.BeginLoadMode();
        SceneManager.LoadScene("GameScene");
    }

    void UpdateContinueButtonVisibility()
    {
        if (continueButtonRoot == null)
            return;

        continueButtonRoot.SetActive(SaveService.HasValidSave());
    }

    static void TryDeleteSavesForNewRun()
    {
        var result = SaveService.DeleteAllSaves();
        if (!result.IsSuccess)
            SaveLogger.LogWarning($"Save delete failed (new run): {result.Message}");
    }
}
