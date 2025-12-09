using System;
using Data;
using TMPro;
using UnityEngine;

public sealed class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] string defaultPlayerId = "player.default";

    [SerializeField] private TMP_Text baseScoreText;
    [SerializeField] private TMP_Text scoreMultiplierText;
    [SerializeField] private TMP_Text criticalChanceText;
    [SerializeField] private TMP_Text criticalMultiplierText;
    [SerializeField] private RectTransform ballDeckContainer;
    [SerializeField] private GameObject ballDeckPrefab;
    private float statUIUpdateCycle = 0.1f;
    private float currentStatUIUpdateCycle = 0f;

    public PlayerInstance Current { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!PlayerRepository.IsInitialized)
        {
            Debug.LogWarning("[PlayerManager] PlayerRepository not initialized. " +
                             "Call PlayerRepository.InitializeFromJson before creating a player.");
            return;
        }

        try
        {
            CreatePlayer(defaultPlayerId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerManager] Failed to create default player '{defaultPlayerId}': {e}");
        }
    }

    public void CreatePlayer(string playerId)
    {
        if (!PlayerRepository.IsInitialized)
        {
            Debug.LogError("[PlayerManager] Cannot create player. PlayerRepository not initialized.");
            return;
        }

        try
        {
            var dto = PlayerRepository.GetOrThrow(playerId);
            Current = new PlayerInstance(dto);

            // 통화 HUD 등이 있으면 여기서 알림
            CurrencyManager.Instance?.OnPlayerCreated(Current);
            Current.BallDeck.OnDeckChanged += HandleBallDeckChanged;
            HandleBallDeckChanged();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerManager] Failed to create player '{playerId}': {e}");
        }
    }

    private void OnDisable()
    {
        Current.BallDeck.OnDeckChanged -= HandleBallDeckChanged;
    }

    void HandleBallDeckChanged()
    {
        if (Current == null || ballDeckContainer == null || ballDeckPrefab == null)
            return;

        for (int i = ballDeckContainer.childCount - 1; i >= 0; i--)
        {
            var child = ballDeckContainer.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }

        foreach (var kv in Current.BallDeck.Counts)
        {
            var ballId = kv.Key;
            if (string.IsNullOrEmpty(ballId))
            {
                Debug.LogError($"[PlayerManager] ballId not found. {ballId}");
                continue;
            }

            var ballCount = kv.Value;
            if (ballCount <= 0)
            {
                Debug.LogError($"[PlayerManager] ballCount Invalid. {ballId}, {ballCount}");
                continue;
            }

            var ballInfo = Instantiate(ballDeckPrefab, ballDeckContainer);
            var ballDeckView = ballInfo.GetComponent<BallDeckView>();
            if (ballDeckView == null)
            {
                Debug.LogError($"[PlayerManager] BallDeckView not found. {ballId}, {kv.Value}");
                continue;
            }

            ballDeckView.UpdateBallDeckView(ballId, ballCount);
        }
    }

    void Update()
    {
        currentStatUIUpdateCycle += Time.deltaTime;
        if (currentStatUIUpdateCycle >= statUIUpdateCycle)
        {
            currentStatUIUpdateCycle = 0f;
            UpdateStatUI();
        }
    }

    void UpdateStatUI()
    {
        baseScoreText.text = $"{Current.ScoreBase}";
        scoreMultiplierText.text = $"x{Current.ScoreMultiplier:N1}";
        criticalChanceText.text = $"{Current.CriticalChance}%";
        criticalMultiplierText.text = $"x{Current.CriticalMultiplier:N1}";
    }

    public void ResetPlayer()
    {
        Current?.ResetData();
    }
}