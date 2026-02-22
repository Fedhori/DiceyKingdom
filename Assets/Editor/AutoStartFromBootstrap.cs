#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutoStartFromBootstrap
{
    static AutoStartFromBootstrap()
    {
        SceneAsset bootstrapSceneAsset = FindBootstrapSceneAsset();
        if (bootstrapSceneAsset == null)
        {
            Debug.LogWarning("[AutoStartFromBootstrap] Bootstrap scene was not found in Build Settings. Play Mode Start Scene was not changed.");
            return;
        }

        EditorSceneManager.playModeStartScene = bootstrapSceneAsset;
        string bootstrapScenePath = AssetDatabase.GetAssetPath(bootstrapSceneAsset);
        Debug.Log($"[AutoStartFromBootstrap] '{bootstrapScenePath}' was set as Play Mode Start Scene.");
    }

    private static SceneAsset FindBootstrapSceneAsset()
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            string scenePath = buildScenes[i].path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (!string.Equals(sceneName, SceneIds.Bootstrap, System.StringComparison.Ordinal))
            {
                continue;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                Debug.LogWarning($"[AutoStartFromBootstrap] Scene asset load failed: {scenePath}");
            }

            return sceneAsset;
        }

        return null;
    }
}
#endif
