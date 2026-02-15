using UnityEngine;

[DefaultExecutionOrder(-9000)]
public sealed class GameSceneInstaller : MonoBehaviour
{
    [SerializeField] GameSceneRefs sceneRefs = new();
    bool ownsRun;

    void Awake()
    {
        var app = GameApp.I;
        if (app == null)
        {
            Debug.LogError("[GameSceneInstaller] GameApp is missing.");
            return;
        }

        app.BeginRun(sceneRefs);
        ownsRun = true;
    }

    void OnDestroy()
    {
        if (!ownsRun)
            return;

        var app = GameApp.I;
        if (app == null)
            return;

        app.EndRun();
    }
}
