using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

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

            
            StaticDataLoader.LoadAll();

            
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

