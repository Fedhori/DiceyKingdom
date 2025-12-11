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
        FlowManager.Instance?.StartRun();
    }

    public void HandleGameOver()
    {
        AudioManager.Instance.Play("GameOver");
        gameOverOverlay.gameObject.SetActive(true);
    }

    public void HandleGameClear()
    {
        AudioManager.Instance.Play("GameClear");
        if (gameClearDescription.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = ScoreManager.Instance.TotalScore.ToString();
        gameClearOverlay.gameObject.SetActive(true);
    }
}