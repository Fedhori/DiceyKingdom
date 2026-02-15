using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

[DefaultExecutionOrder(-10000)]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameObject managersRoot;

    async void Awake()
    {
        Application.targetFrameRate = 60;
        // 0) 혹시 이미 켜져 있으면 꺼두기(중복 대비)
        if (managersRoot && managersRoot.activeSelf) managersRoot.SetActive(false);

        // 1) 캐시/데이터 준비
        await SaCache.InitAsync(new SaOptions {
            forceRefresh = Debug.isDebugBuild,
            refreshIfAppVersionChanged = true,
            verifyHash = true
        });

        bool loadedConfig = await GameConfigProvider.LoadFromStreamingAssetsAsync();
        if (!loadedConfig)
        {
            Debug.LogError("[Bootstrap] GameConfig loading failed. Bootstrap halted.");
            return;
        }

        try
        {
            StaticDataLoader.LoadAll();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Bootstrap] Static data loading failed.\n{ex}");
            return;
        }

        // 2) 매니저들 활성
        if (managersRoot)
        {
            managersRoot.SetActive(true);
            await Task.Yield();
        }

        var gameApp = GameApp.I;
        if (gameApp == null)
        {
            if (managersRoot != null)
            {
                gameApp = managersRoot.GetComponent<GameApp>();
                if (gameApp == null)
                    gameApp = managersRoot.AddComponent<GameApp>();
            }
            else
            {
                gameApp = FindFirstObjectByType<GameApp>(FindObjectsInactive.Include);
                if (gameApp == null)
                {
                    var appObject = new GameObject(nameof(GameApp));
                    gameApp = appObject.AddComponent<GameApp>();
                }
            }
        }

        if (gameApp != null)
        {
            gameApp.RebuildServices();
        }

        await SaveWebGlSync.SyncFromPersistentAsync();

        // 3) 다음 씬으로
        await SceneManager.LoadSceneAsync("GameScene").AsTask();
    }
}

public static class AsyncOperationExt
{
    public static async Task AsTask(this AsyncOperation op)
    {
        while (!op.isDone) await Task.Yield();
    }
}
