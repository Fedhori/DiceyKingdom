using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Unity component that provides audio runtime behavior.
/// </summary>
public class AudioService : MonoBehaviour
{
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
    [Tooltip("true硫?紐⑤뱺 SFX???쒗븳 ?곸슜. false硫?limitedKeys?먮쭔 ?곸슜")]
    public bool limitAllSfx = false;
    public string[] limitedKeys;
    [Tooltip("媛숈? ???ъ깮 理쒖냼 媛꾧꺽(珥?. 0.08~0.12 沅뚯옣")]
    [Range(0f, 0.5f)] public float sfxMinIntervalSec = 0.10f;
    [Tooltip("媛숈? ?ㅼ쓽 理쒕? ?숈떆 ?ъ깮 ??蹂댁씠??. 3~5 沅뚯옣")]
    [Range(1, 16)] public int sfxMaxVoices = 4;

    // --- Internal tables ---
    private Dictionary<string, SfxEntry> table;
    private HashSet<string> limitedSet;

    // --- Limiter state ---
    private readonly Dictionary<string, float> _nextAllowed = new(); // key->time(unscaled)
    private readonly Dictionary<string, int>   _voices      = new(); // key->playing count

    void Awake()
    {
        table = new Dictionary<string, SfxEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries) if (!string.IsNullOrEmpty(e.key)) table[e.key] = e;

        // ?쒗븳 ?곸슜 ??吏묓빀
        limitedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (limitedKeys != null)
        {
            foreach (var k in limitedKeys)
                if (!string.IsNullOrEmpty(k)) limitedSet.Add(k);
        }
    }

    // --- Public SFX API ------------------------------------------------------

    /// <summary>
    /// ?쇰컲 ?ъ깮(?꾩슂 ???쒗븳 ?곸슜)
    /// </summary>
    public void Play(string key)
    {
        if (!table.TryGetValue(key, out var e) || e.clip == null) return;

        if (ShouldLimit(key))
        {
            float now = Time.unscaledTime;
            if (!LimiterCanPlay(key, now)) return;

            // ?ъ깮
            src.PlayOneShot(e.clip, e.volume <= 0f ? 1f : e.volume);

            // ?쒖옉/醫낅즺 愿由?OneShot? ?대깽?멸? ?놁쑝??湲몄씠濡?醫낅즺 ?덉빟)
            LimiterOnStart(key, now);
            float dur = Mathf.Max(0.02f, e.clip.length / Mathf.Max(0.01f, src.pitch));
            StartCoroutine(ReleaseVoiceAfter(key, dur));
        }
        else
        {
            src?.PlayOneShot(e.clip, e.volume <= 0f ? 1f : e.volume);
        }
    }

    // ?꾩슂?섎㈃ 肄붾뱶濡???異붽?
    public void AddLimitedKey(string key)
    {
        if (!string.IsNullOrEmpty(key)) limitedSet.Add(key);
    }

    // -------------------------------------------------------------------------

    private bool ShouldLimit(string key) => limitAllSfx || (limitedSet != null && limitedSet.Contains(key));

    private bool LimiterCanPlay(string key, float now)
    {
        // 理쒖냼 媛꾧꺽
        if (_nextAllowed.TryGetValue(key, out var t) && now < t) return false;

        // ?숈떆 蹂댁씠??
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
        // ??꾩뒪耳??臾댁떆(寃뚯엫 ?щ줈/?쇱떆?뺤??먮룄 ?뺥솗)
        yield return new WaitForSecondsRealtime(seconds);
        if (_voices.TryGetValue(key, out var v))
            _voices[key] = Mathf.Max(0, v - 1);
    }
}


