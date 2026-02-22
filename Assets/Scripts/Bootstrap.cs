using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Game.Infrastructure.Data;

[DefaultExecutionOrder(-20000)]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameObject managersRoot;

    async void Awake()
    {
        try
        {
            Application.targetFrameRate = 60;

            
            if (managersRoot && managersRoot.activeSelf)
                managersRoot.SetActive(false);

            
            await SaCache.InitAsync(new SaOptions
            {
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

            GameDataBuildMode dataMode = Debug.isDebugBuild
                ? GameDataBuildMode.Development
                : GameDataBuildMode.Release;

            GameDataBuildResult dataResult = GameDataRuntime.LoadAtStartup(dataMode);
            if (dataResult.shouldBlockStartup)
            {
                Debug.LogError("[Bootstrap] Game data validation failed. Bootstrap halted.");
                return;
            }

            
            if (managersRoot == null)
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

            
            await SaveWebGlSync.SyncFromPersistentAsync();

            await SceneManager.LoadSceneAsync(SceneIds.TemplateStartScene).AsTask();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Bootstrap] Bootstrap failed.\n{ex}");
        }
    }
}




public static class AsyncOperationExt
{
    public static async Task AsTask(this AsyncOperation op)
    {
        while (!op.isDone) await Task.Yield();
    }
}

