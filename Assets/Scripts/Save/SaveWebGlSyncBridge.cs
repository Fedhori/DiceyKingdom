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
        instance = null;
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
