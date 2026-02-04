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

        // 2) 매니저들 활성 + DDoL
        if (managersRoot)
        {
            managersRoot.SetActive(true);              // => 자식 매니저들 Awake() 즉시 호출
            DontDestroyOnLoad(managersRoot);           // 전 씬 공통 상주
            await Task.Yield();                        // 한 프레임 양보 → Start()까지 보장하려면 추가로 한 번 더
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
