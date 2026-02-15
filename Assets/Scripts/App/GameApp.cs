using UI;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class GameApp : MonoBehaviour
{
    public static GameApp I { get; private set; }

    [Header("UI")]
    [SerializeField] TooltipManager tooltip;
    [SerializeField] ModalManager modal;
    [SerializeField] OptionManager option;
    [SerializeField] FloatingTextManager floatingText;
    [SerializeField] ToastManager toast;

    [Header("App Services")]
    [SerializeField] AudioManager audioManager;
    [SerializeField] BgmManager bgm;
    [SerializeField] InputManager input;
    [SerializeField] GameSpeedManager gameSpeed;
    [SerializeField] ParticleManager particle;
    [SerializeField] SaveManager save;
    [SerializeField] StaticDataManager staticData;
    [SerializeField] DevCommandManager devCommand;

    public AppServices App { get; private set; }
    public RunServices Run { get; private set; }
    public UIService UI => App?.UI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        I = null;
    }

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        EnsurePersistentUiHierarchy();
        DontDestroyOnLoad(gameObject);
        BuildServices();
    }

    void OnDestroy()
    {
        if (I != this)
            return;

        EndRun();
        I = null;
    }

    public void BeginRun(GameSceneRefs sceneRefs)
    {
        EndRun();
        Run = new RunServices(sceneRefs);
    }

    public void EndRun()
    {
        if (Run == null)
            return;

        Run.Dispose();
        Run = null;
    }

    public void RebuildServices()
    {
        BuildServices();
    }

    void BuildServices()
    {
        var ui = new UIService(tooltip, modal, option, floatingText, toast);
        App = new AppServices(ui, audioManager, bgm, input, gameSpeed, particle, save, staticData, devCommand);
    }

    void EnsurePersistentUiHierarchy()
    {
        tooltip?.EnsurePersistentHierarchy(transform);
        modal?.EnsurePersistentHierarchy(transform);
        option?.EnsurePersistentHierarchy(transform);
        floatingText?.EnsurePersistentHierarchy(transform);
        toast?.EnsurePersistentHierarchy(transform);
    }
}
