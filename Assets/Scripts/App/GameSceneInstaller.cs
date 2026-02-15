using UnityEngine;

[DefaultExecutionOrder(-9000)]
public sealed class GameSceneInstaller : MonoBehaviour
{
    [SerializeField] GameSceneRefs sceneRefs = new();
    bool ownsRun;
    RunServices startedRun;

    void Awake()
    {
        var app = GameApp.I;
        if (app == null)
        {
            Debug.LogError("[GameSceneInstaller] GameApp is missing.");
            return;
        }

        if (app.Run != null)
            return;

        app.BeginRun(sceneRefs);
        ownsRun = true;
        startedRun = app.Run;
    }

    void OnDestroy()
    {
        if (!ownsRun)
            return;

        var app = GameApp.I;
        if (app == null)
            return;

        if (ReferenceEquals(app.Run, startedRun))
            app.EndRun();
    }
}
