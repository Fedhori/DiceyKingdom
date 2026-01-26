using UnityEngine;
using UnityEngine.Scripting;

public sealed class SaveWebGlSyncBridge : MonoBehaviour
{
    public const string GameObjectName = "SaveWebGlSyncBridge";
    static SaveWebGlSyncBridge instance;

    public static SaveWebGlSyncBridge Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Create();
#endif
    }

    public static void Create()
    {
        if (instance != null)
            return;

        var go = new GameObject(GameObjectName);
        instance = go.AddComponent<SaveWebGlSyncBridge>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.name = GameObjectName;
        DontDestroyOnLoad(gameObject);
    }

    [Preserve]
    public void OnWebGlSyncFromCompleted(string error)
    {
        SaveWebGlSync.NotifySyncFromResult(error);
    }

    [Preserve]
    public void OnWebGlSyncToCompleted(string error)
    {
        SaveWebGlSync.NotifySyncToResult(error);
    }
}
