using System.Threading.Tasks;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Game.Save
{
    public static class SaveWebGlSync
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void SaveSyncToPersistent();

        [DllImport("__Internal")]
        static extern void SaveSyncFromPersistent();
    #endif

        public static Task<bool> SyncFromPersistentAsync()
        {
    #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                SaveSyncFromPersistent();
                return Task.FromResult(true);
            }
            catch (System.Exception ex)
            {
                SaveLogger.LogWarning($"WebGL sync-from failed: {ex}");
                return Task.FromResult(false);
            }
    #else
            return Task.FromResult(true);
    #endif
        }

        public static void SyncToPersistent()
        {
    #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                SaveSyncToPersistent();
            }
            catch (System.Exception ex)
            {
                SaveLogger.LogWarning($"WebGL sync-to failed: {ex}");
            }
    #endif
        }
    }
}
