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
        ResolveReferencesIfNeeded();
        var ui = new UIService(tooltip, modal, option, floatingText, toast);
        App = new AppServices(ui, audioManager, bgm, input, gameSpeed, particle, save, staticData, devCommand);
    }

    void ResolveReferencesIfNeeded()
    {
        if (tooltip == null)
            tooltip = GetComponentInChildren<TooltipManager>(true);
        if (modal == null)
            modal = GetComponentInChildren<ModalManager>(true);
        if (option == null)
            option = GetComponentInChildren<OptionManager>(true);
        if (floatingText == null)
            floatingText = GetComponentInChildren<FloatingTextManager>(true);
        if (toast == null)
            toast = GetComponentInChildren<ToastManager>(true);

        if (audioManager == null)
            audioManager = GetComponentInChildren<AudioManager>(true);
        if (bgm == null)
            bgm = GetComponentInChildren<BgmManager>(true);
        if (input == null)
            input = GetComponentInChildren<InputManager>(true);
        if (gameSpeed == null)
            gameSpeed = GetComponentInChildren<GameSpeedManager>(true);
        if (particle == null)
            particle = GetComponentInChildren<ParticleManager>(true);
        if (save == null)
            save = GetComponentInChildren<SaveManager>(true);
        if (staticData == null)
            staticData = GetComponentInChildren<StaticDataManager>(true);
        if (devCommand == null)
            devCommand = GetComponentInChildren<DevCommandManager>(true);
    }
}
