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
        ItemManager.Instance?.InitializeFromPlayer(PlayerManager.Instance?.Current);
        StageManager.Instance?.StartRun();
    }

    public void HandleGameOver()
    {
        AudioManager.Instance.Play("GameOver");
        gameOverOverlay.SetActive(true);
        GameSpeedManager.Instance.ForcePaused = true;
    }

    public void HandleGameClear()
    {
        AudioManager.Instance.Play("GameClear");
        gameClearOverlay.SetActive(true);
        GameSpeedManager.Instance.ForcePaused = true;
    }
}
