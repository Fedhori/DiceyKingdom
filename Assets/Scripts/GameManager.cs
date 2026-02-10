using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public System.Random Rng { get; private set; } = new System.Random();
    public GameStaticDataCatalog staticDataCatalog { get; private set; }
    public GameStartingLoadout startingLoadout { get; private set; }
    public GameTurnRuntime turnRuntime { get; private set; }

    async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        await SaCache.Ready;

        TryInitializeGameStaticData();
        EnsureRuntimePlayUi();
    }

    public void ResetRng(int seed)
    {
        Rng = new System.Random(seed);
        turnRuntime?.SetRandom(Rng);
    }

    public bool TryLoad(out string payloadJson)
    {
        payloadJson = string.Empty;

        var save = SaveManager.Instance;
        if (save == null)
            return false;

        return save.TryLoad(out payloadJson);
    }

    public bool Save(string payloadJson)
    {
        var save = SaveManager.Instance;
        if (save == null)
            return false;

        return save.Save(payloadJson);
    }

    public bool TryGenerateStartingLoadout(int totalDiceCount, out GameStartingLoadout generatedLoadout)
    {
        generatedLoadout = null;
        if (staticDataCatalog == null)
            return false;

        if (!GameStartingLoadoutBuilder.TryBuild(staticDataCatalog, Rng, totalDiceCount, out var loadout, out _))
            return false;

        generatedLoadout = loadout;
        return true;
    }

    public bool TryAdvanceTurnPhase()
    {
        if (turnRuntime == null)
            return false;

        return turnRuntime.TryAdvancePhase();
    }

    public bool TryAssignDieToSituation(int dieIndex, int? situationInstanceId)
    {
        if (turnRuntime == null)
            return false;

        return turnRuntime.TryAssignDieToSituation(dieIndex, situationInstanceId);
    }

    public bool TryRestartRun()
    {
        if (staticDataCatalog == null || turnRuntime == null)
            return false;

        var totalDiceCount = startingLoadout != null ? startingLoadout.totalDiceCount : 10;
        if (!GameStartingLoadoutBuilder.TryBuild(staticDataCatalog, Rng, totalDiceCount, out var rebuiltLoadout, out var loadoutError))
        {
            Debug.LogError($"[GameManager] Failed to restart run.\n{loadoutError}");
            return false;
        }

        startingLoadout = rebuiltLoadout;
        turnRuntime.StartNewRun(startingLoadout);
        return true;
    }

    void TryInitializeGameStaticData()
    {
        if (!GameStaticDataLoader.TryLoadDefault(out var loadedCatalog, out var errorMessage))
        {
            Debug.LogError($"[GameManager] Failed to load static game data.\n{errorMessage}");
            return;
        }

        staticDataCatalog = loadedCatalog;

        if (!GameStartingLoadoutBuilder.TryBuild(staticDataCatalog, Rng, 10, out var builtLoadout, out var loadoutError))
        {
            Debug.LogError($"[GameManager] Failed to build starting loadout.\n{loadoutError}");
            return;
        }

        startingLoadout = builtLoadout;
        turnRuntime = new GameTurnRuntime(Rng, staticDataCatalog);
        turnRuntime.StartNewRun(startingLoadout);
        Debug.Log($"[GameManager] Static game data loaded. situations={staticDataCatalog.situations.Count}, advisors={staticDataCatalog.advisors.Count}, decrees={staticDataCatalog.decrees.Count}, diceUpgrades={staticDataCatalog.diceUpgrades.Count}");
    }

    void EnsureRuntimePlayUi()
    {
        var legacyDebugUi = GetComponent<GameRuntimeDebugUi>();
        if (legacyDebugUi != null)
            Destroy(legacyDebugUi);

        if (GetComponent<GameRuntimePlayUi>() == null)
            gameObject.AddComponent<GameRuntimePlayUi>();
    }
}
