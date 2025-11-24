using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public System.Random Rng { get; private set; } = new System.Random();

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HandleGameStart();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void HandleGameStart()
    {
    }
}