using UnityEngine;

namespace Game.App
{
    [DefaultExecutionOrder(-9000)]



    public sealed class GameSceneInstaller : MonoBehaviour
    {
        [SerializeField] GameSceneRefs sceneRefs = new();
        [SerializeField] bool useFixedSeed;
        [SerializeField] int fixedSeed = 1001;
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

            if (app.Run == null)
            {
                app.BeginRun(sceneRefs);
                ownsRun = true;
                startedRun = app.Run;
            }

            RunServices run = app.Run;
            if (run == null)
            {
                Debug.LogError("[GameSceneInstaller] BeginRun failed. RunServices is null.");
                return;
            }

            run.InitializeRunIfNeeded(useFixedSeed ? fixedSeed : null);
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

}
