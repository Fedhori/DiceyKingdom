using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable] public struct SfxEntry {
        public string key;
        [Range(0f,1f)] public float volume;
        public AudioClip clip;
    }

    // --- SFX ---
    [Header("SFX")]
    public AudioSource src;
    public SfxEntry[] entries;

    [Header("SFX Limiter")]
    [Tooltip("true면 모든 SFX에 제한 적용. false면 limitedKeys에만 적용")]
    public bool limitAllSfx = false;
    public string[] limitedKeys;
    [Tooltip("같은 키 재생 최소 간격(초). 0.08~0.12 권장")]
    [Range(0f, 0.5f)] public float sfxMinIntervalSec = 0.10f;
    [Tooltip("같은 키의 최대 동시 재생 수(보이스). 3~5 권장")]
    [Range(1, 16)] public int sfxMaxVoices = 4;

    // --- Internal tables ---
    private Dictionary<string, SfxEntry> table;
    private HashSet<string> limitedSet;

    // --- Limiter state ---
    private readonly Dictionary<string, float> _nextAllowed = new(); // key->time(unscaled)
    private readonly Dictionary<string, int>   _voices      = new(); // key->playing count

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        table = new Dictionary<string, SfxEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries) if (!string.IsNullOrEmpty(e.key)) table[e.key] = e;

        // 제한 적용 키 집합
        limitedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (limitedKeys != null)
        {
            foreach (var k in limitedKeys)
                if (!string.IsNullOrEmpty(k)) limitedSet.Add(k);
        }
    }

    // --- Public SFX API ------------------------------------------------------

    /// <summary>
    /// 일반 재생(필요 시 제한 적용)
    /// </summary>
    public void Play(string key)
    {
        if (!table.TryGetValue(key, out var e) || e.clip == null) return;

        if (ShouldLimit(key))
        {
            float now = Time.unscaledTime;
            if (!LimiterCanPlay(key, now)) return;

            // 재생
            src.PlayOneShot(e.clip, e.volume <= 0f ? 1f : e.volume);

            // 시작/종료 관리(OneShot은 이벤트가 없으니 길이로 종료 예약)
            LimiterOnStart(key, now);
            float dur = Mathf.Max(0.02f, e.clip.length / Mathf.Max(0.01f, src.pitch));
            StartCoroutine(ReleaseVoiceAfter(key, dur));
        }
        else
        {
            src?.PlayOneShot(e.clip, e.volume <= 0f ? 1f : e.volume);
        }
    }

    // 필요하면 코드로 키 추가
    public void AddLimitedKey(string key)
    {
        if (!string.IsNullOrEmpty(key)) limitedSet.Add(key);
    }

    // -------------------------------------------------------------------------

    private bool ShouldLimit(string key) => limitAllSfx || (limitedSet != null && limitedSet.Contains(key));

    private bool LimiterCanPlay(string key, float now)
    {
        // 최소 간격
        if (_nextAllowed.TryGetValue(key, out var t) && now < t) return false;

        // 동시 보이스
        _voices.TryGetValue(key, out var v);
        return v < Mathf.Max(1, sfxMaxVoices);
    }

    private void LimiterOnStart(string key, float now)
    {
        _nextAllowed[key] = now + Mathf.Max(0f, sfxMinIntervalSec);
        _voices[key] = (_voices.TryGetValue(key, out var v) ? v : 0) + 1;
    }

    private System.Collections.IEnumerator ReleaseVoiceAfter(string key, float seconds)
    {
        // 타임스케일 무시(게임 슬로/일시정지에도 정확)
        yield return new WaitForSecondsRealtime(seconds);
        if (_voices.TryGetValue(key, out var v))
            _voices[key] = Mathf.Max(0, v - 1);
    }
}
