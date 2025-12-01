using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
}