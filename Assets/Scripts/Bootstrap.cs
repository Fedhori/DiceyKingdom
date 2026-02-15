using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

[DefaultExecutionOrder(-20000)]
/// <summary>
/// Bootstraps startup by loading core data, activating app scope, and entering the game scene.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameObject managersRoot;

    async void Awake()
    {
        try
        {
            Application.targetFrameRate = 60;

            // 0) ?뱀떆 ?대? 耳쒖졇 ?덉쑝硫?爰쇰몢湲?以묐났 ?鍮?
            if (managersRoot && managersRoot.activeSelf)
                managersRoot.SetActive(false);

            // 1) SaCache
            await SaCache.InitAsync(new SaOptions
            {
                forceRefresh = Debug.isDebugBuild,
                refreshIfAppVersionChanged = true,
                verifyHash = true
            });

            // 2) GameConfigProvider
            bool loadedConfig = await GameConfigProvider.LoadFromStreamingAssetsAsync();
            if (!loadedConfig)
            {
                Debug.LogError("[Bootstrap] GameConfig loading failed. Bootstrap halted.");
                return;
            }

            // 3) StaticDataLoader
            StaticDataLoader.LoadAll();

            // 4) managersRoot ?쒖꽦??            if (managersRoot == null)
            {
                Debug.LogError("[Bootstrap] managersRoot is missing. Bootstrap halted.");
                return;
            }

            managersRoot.SetActive(true);
            await Task.Yield();

            var gameApp = managersRoot.GetComponentInChildren<GameApp>(true);
            if (gameApp == null)
            {
                Debug.LogError("[Bootstrap] GameApp is missing under managersRoot. Bootstrap halted.");
                return;
            }

            gameApp.RebuildServices();

            // 5) SaveWebGlSync
            await SaveWebGlSync.SyncFromPersistentAsync();

            // 6) ?ㅼ쓬 ?ъ쑝濡?            await SceneManager.LoadSceneAsync(SceneIds.GameScene).AsTask();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Bootstrap] Bootstrap failed.\n{ex}");
        }
    }
}

/// <summary>
/// Provides async helpers for awaiting Unity AsyncOperation instances.
/// </summary>
public static class AsyncOperationExt
{
    public static async Task AsTask(this AsyncOperation op)
    {
        while (!op.isDone) await Task.Yield();
    }
}

