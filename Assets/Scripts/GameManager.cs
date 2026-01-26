using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameObject gameOverOverlay;
    [SerializeField] private GameObject gameClearOverlay;
    [SerializeField] private LocalizeStringEvent gameClearDescription;

    public System.Random Rng { get; private set; } = new System.Random();

    void Awake()
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
        HandleGameStart();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    void HandleGameStart()
    {
        var save = SaveManager.Instance;
        if (save != null && save.IsLoadMode)
        {
            if (!HandleLoadGame())
                save.EndLoadMode();
            return;
        }

        ItemManager.Instance?.InitializeFromPlayer(PlayerManager.Instance?.Current);
        StageManager.Instance?.StartRun();
    }

    public void HandleGameOver()
    {
        var deleteResult = SaveService.DeleteAllSaves();
        if (!deleteResult.IsSuccess)
            SaveLogger.LogWarning($"Save delete failed (game over): {deleteResult.Message}");

        AudioManager.Instance.Play("GameOver");
        gameOverOverlay.SetActive(true);
        GameSpeedManager.Instance.ForcePaused = true;
    }

    public void HandleGameClear()
    {
        var deleteResult = SaveService.DeleteAllSaves();
        if (!deleteResult.IsSuccess)
            SaveLogger.LogWarning($"Save delete failed (game clear): {deleteResult.Message}");

        AudioManager.Instance.Play("GameClear");
        gameClearOverlay.SetActive(true);
        GameSpeedManager.Instance.ForcePaused = true;
    }

    public void ResetRng(int seed)
    {
        Rng = new System.Random(seed);
    }

    bool HandleLoadGame()
    {
        var save = SaveManager.Instance;
        if (save == null)
            return false;

        var data = SaveService.ReadSaveWithBackup(out var result, out var usedBackup);
        if (data == null)
        {
            SaveLogger.LogError($"Load failed: {result.Message}");
            var modal = ModalManager.Instance;
            modal?.ShowInfo(
                "modal",
                "modal.loadFailed.title",
                "modal",
                "modal.loadFailed.message",
                () => { });
            return false;
        }

        if (usedBackup)
        {
            var modal = ModalManager.Instance;
            modal?.ShowInfo(
                "modal",
                "modal.loadBackup.title",
                "modal",
                "modal.loadBackup.message",
                () => { });
        }

        bool applied = save.ApplySaveData(data);
        if (!applied)
        {
            SaveLogger.LogError("Load failed: ApplySaveData returned false.");
            return false;
        }

        StageManager.Instance?.StartRunFromIndex(data.run?.stageIndex ?? 0);
        save.EndLoadMode();
        return true;
    }
}
