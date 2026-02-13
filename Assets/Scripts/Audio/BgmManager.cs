using UnityEngine;

public sealed class BgmManager : MonoBehaviour
{
    public static BgmManager Instance { get; private set; }

    const string PrefsKeyBaseVolume = "bgm.baseVolume";

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip mainBgm;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool playOnStart = true;

    [Header("Muffle")]
    [SerializeField] private AudioLowPassFilter lowPassFilter;
    [SerializeField] private float normalCutoff = 22000f;
    [SerializeField] private float muffledCutoff = 900f;

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
        {
            Debug.LogWarning("[BgmManager] AudioSource is not assigned.", this);
            return;
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        if (mainBgm != null)
            audioSource.clip = mainBgm;

        if (lowPassFilter == null)
            lowPassFilter = GetComponent<AudioLowPassFilter>();
        if (lowPassFilter == null)
        {
            Debug.LogWarning("[BgmManager] AudioLowPassFilter is not assigned.", this);
            return;
        }

        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = normalCutoff;

        baseVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKeyBaseVolume, volume));
    }

    void Start()
    {
        ApplyVolume();
        if (playOnStart)
            Play();
    }

    public float BaseVolume => baseVolume;

    public void SetBaseVolume(float value)
    {
        baseVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefsKeyBaseVolume, baseVolume);
        PlayerPrefs.Save();
        ApplyVolume();
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

    public void SetMuffled(bool muffled)
    {
        if (lowPassFilter == null)
            return;

        if (isMuffled == muffled)
            return;

        isMuffled = muffled;
        lowPassFilter.cutoffFrequency = muffled ? muffledCutoff : normalCutoff;
    }

    void ApplyVolume()
    {
        if (audioSource == null)
            return;

        audioSource.volume = Mathf.Clamp01(baseVolume);
    }
}
