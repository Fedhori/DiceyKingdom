using UI;
using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-10000)]
public sealed class GameApp : MonoBehaviour
{
    public static GameApp I { get; private set; }

    [Header("UI")]
    [SerializeField] TooltipService tooltip;
    [SerializeField] ModalService modal;
    [SerializeField] OptionService option;
    [SerializeField] FloatingTextService floatingText;
    [SerializeField] ToastService toast;

    [Header("App Services")]
    [SerializeField] AudioService audioService;
    [SerializeField] BgmService bgm;
    [SerializeField] InputService input;
    [SerializeField] GameSpeedService gameSpeed;
    [SerializeField] ParticleService particle;
    [SerializeField] SaveRuntimeService save;
    [SerializeField] StaticDataService staticData;
    [SerializeField] DevCommandService devCommand;

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
        if (!ValidateAppWiring())
        {
            Debug.LogError("[GameApp] App wiring is invalid. Fix inspector references/hierarchy in Bootstrap scene.");
            enabled = false;
            return;
        }

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
        App = new AppServices(ui, audioService, bgm, input, gameSpeed, particle, save, staticData, devCommand);
    }

    bool ValidateAppWiring()
    {
        bool valid = true;

        if (tooltip == null)
        {
            Debug.LogError("[GameApp] tooltip is not assigned.");
            valid = false;
        }
        else
        {
            valid &= tooltip.ValidateConfiguration(transform);
        }

        if (modal == null)
        {
            Debug.LogError("[GameApp] modal is not assigned.");
            valid = false;
        }
        else
        {
            valid &= modal.ValidateConfiguration(transform);
        }

        if (option == null)
        {
            Debug.LogError("[GameApp] option is not assigned.");
            valid = false;
        }
        else
        {
            valid &= option.ValidateConfiguration(transform);
        }

        if (floatingText == null)
        {
            Debug.LogError("[GameApp] floatingText is not assigned.");
            valid = false;
        }
        else
        {
            valid &= floatingText.ValidateConfiguration(transform);
        }

        if (toast == null)
        {
            Debug.LogError("[GameApp] toast is not assigned.");
            valid = false;
        }
        else
        {
            valid &= toast.ValidateConfiguration(transform);
        }

        return valid;
    }
}

