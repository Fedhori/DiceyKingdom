#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class AutoStartFromBootstrap
{
    
    private const string mainScenePath = "Assets/Scenes/Bootstrap.unity";

    static AutoStartFromBootstrap()
    {
        EditorApplication.playModeStateChanged += ChangePlayMode;
    }

    private static void ChangePlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string currentScene = SceneManager.GetActiveScene().path;
            if (currentScene != mainScenePath)
            {
                Debug.Log($"[AutoStart] Switching to main scene: {mainScenePath}");
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(mainScenePath);
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
#endif