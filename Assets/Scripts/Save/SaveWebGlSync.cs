using System;
using System.Threading.Tasks;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class SaveWebGlSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void SaveSyncToPersistent();

    [DllImport("__Internal")]
    static extern void SaveSyncFromPersistent();
#endif

    static TaskCompletionSource<bool> syncFromTcs;

    public static Task<bool> SyncFromPersistentAsync()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (syncFromTcs != null)
            return syncFromTcs.Task;

        syncFromTcs = new TaskCompletionSource<bool>();
        EnsureBridge();

        try
        {
            SaveSyncFromPersistent();
        }
        catch (Exception ex)
        {
            SaveLogger.LogWarning($"WebGL sync-from failed: {ex}");
            syncFromTcs.TrySetResult(false);
            syncFromTcs = null;
        }

        return syncFromTcs.Task;
#else
        return Task.FromResult(true);
#endif
    }

    public static void SyncToPersistent()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureBridge();
        try
        {
            SaveSyncToPersistent();
        }
        catch (Exception ex)
        {
            SaveLogger.LogWarning($"WebGL sync-to failed: {ex}");
        }
#endif
    }

    internal static void NotifySyncFromResult(string error)
    {
        if (!string.IsNullOrEmpty(error))
            SaveLogger.LogWarning(error);

        syncFromTcs?.TrySetResult(string.IsNullOrEmpty(error));
        syncFromTcs = null;
    }

    internal static void NotifySyncToResult(string error)
    {
        if (!string.IsNullOrEmpty(error))
            SaveLogger.LogWarning(error);
    }

    static void EnsureBridge()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (SaveWebGlSyncBridge.Instance != null)
            return;

        SaveWebGlSyncBridge.Create();
#endif
    }
}
