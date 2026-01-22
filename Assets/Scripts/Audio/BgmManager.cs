using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BgmManager : MonoBehaviour
{
    public static BgmManager Instance { get; private set; }

    const string PrefsKeyBaseVolume = "bgm.baseVolume";

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip mainBgm;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool playOnStart = true;

    [Header("Muffle (Shop)")]
    [SerializeField] private AudioLowPassFilter lowPassFilter;
    [SerializeField] private float normalCutoff = 22000f;
    [SerializeField] private float muffledCutoff = 900f;

    StageManager subscribedStageManager;
    bool isMuffled;
    float baseVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        if (mainBgm != null)
            audioSource.clip = mainBgm;

        if (lowPassFilter == null)
            lowPassFilter = GetComponent<AudioLowPassFilter>();
        if (lowPassFilter == null)
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();

        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = normalCutoff;

        baseVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKeyBaseVolume, volume));
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleSceneChanged;
        SubscribeStageManager();
        RefreshVolumeForScene(SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        if (playOnStart)
            Play();

        RefreshMuffleState();
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleSceneChanged;
        UnsubscribeStageManager();
    }

    void SubscribeStageManager()
    {
        UnsubscribeStageManager();

        var stageManager = StageManager.Instance;
        if (stageManager == null)
            return;

        subscribedStageManager = stageManager;
        subscribedStageManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void UnsubscribeStageManager()
    {
        if (subscribedStageManager == null)
            return;

        subscribedStageManager.OnPhaseChanged -= HandlePhaseChanged;
        subscribedStageManager = null;
    }

    void HandlePhaseChanged(StagePhase phase)
    {
        SetMuffled(phase != StagePhase.Play);
    }

    void HandleSceneChanged(Scene previous, Scene next)
    {
        _ = previous;
        UnsubscribeStageManager();
        SubscribeStageManager();
        RefreshMuffleState();
        RefreshVolumeForScene(next.name);
    }

    void RefreshMuffleState()
    {
        var stageManager = StageManager.Instance;
        SetMuffled(stageManager != null && stageManager.CurrentPhase != StagePhase.Play);
    }

    void RefreshVolumeForScene(string sceneName)
    {
        if (audioSource == null)
            return;

        float multiplier = sceneName == "MainMenuScene" ? 0.5f : 1f;
        audioSource.volume = Mathf.Clamp01(baseVolume * multiplier);
    }

    public float BaseVolume => baseVolume;

    public void SetBaseVolume(float value)
    {
        baseVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefsKeyBaseVolume, baseVolume);
        PlayerPrefs.Save();
        RefreshVolumeForScene(SceneManager.GetActiveScene().name);
    }

    public void Play()
    {
        if (audioSource == null)
            return;

        if (audioSource.clip == null && mainBgm != null)
            audioSource.clip = mainBgm;

        if (audioSource.isPlaying || audioSource.clip == null)
            return;

        audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    void SetMuffled(bool muffled)
    {
        if (lowPassFilter == null)
            return;

        if (isMuffled == muffled)
            return;

        isMuffled = muffled;
        lowPassFilter.cutoffFrequency = muffled ? muffledCutoff : normalCutoff;
    }
}
