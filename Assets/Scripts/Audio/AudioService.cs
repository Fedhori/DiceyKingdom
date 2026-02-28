using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    public class AudioService : MonoBehaviour
    {
        [Serializable]
        public struct SfxEntry
        {
            public string key;
            [Range(0f, 1f)] public float volume;
            public AudioClip clip;
        }

        [Header("SFX")]
        public AudioSource src;
        public SfxEntry[] entries;

        [Header("SFX Limiter")]
        [Tooltip("If true, limiter applies to all SFX. If false, applies only to limitedKeys.")]
        public bool limitAllSfx;
        public string[] limitedKeys;
        [Tooltip("Minimum interval between same key plays (seconds). Recommended: 0.08~0.12.")]
        [Range(0f, 0.5f)] public float sfxMinIntervalSec = 0.10f;
        [Tooltip("Maximum simultaneous voices for same key. Recommended: 3~5.")]
        [Range(1, 16)] public int sfxMaxVoices = 4;

        Dictionary<string, SfxEntry> table;
        HashSet<string> limitedSet;

        readonly Dictionary<string, float> nextAllowed = new();
        readonly Dictionary<string, int> voices = new();

        void Awake()
        {
            table = new Dictionary<string, SfxEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (SfxEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    table[entry.key] = entry;
                }
            }

            limitedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (limitedKeys != null)
            {
                foreach (string key in limitedKeys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        limitedSet.Add(key);
                    }
                }
            }
        }

        public void Play(string key)
        {
            if (!table.TryGetValue(key, out SfxEntry entry) || entry.clip == null)
            {
                return;
            }

            if (ShouldLimit(key))
            {
                float now = Time.unscaledTime;
                if (!LimiterCanPlay(key, now))
                {
                    return;
                }

                src.PlayOneShot(entry.clip, entry.volume <= 0f ? 1f : entry.volume);
                LimiterOnStart(key, now);
                float duration = Mathf.Max(0.02f, entry.clip.length / Mathf.Max(0.01f, src.pitch));
                StartCoroutine(ReleaseVoiceAfter(key, duration));
                return;
            }

            src?.PlayOneShot(entry.clip, entry.volume <= 0f ? 1f : entry.volume);
        }

        public void AddLimitedKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                limitedSet.Add(key);
            }
        }

        bool ShouldLimit(string key)
        {
            return limitAllSfx || (limitedSet != null && limitedSet.Contains(key));
        }

        bool LimiterCanPlay(string key, float now)
        {
            if (nextAllowed.TryGetValue(key, out float nextTime) && now < nextTime)
            {
                return false;
            }

            voices.TryGetValue(key, out int activeVoices);
            return activeVoices < Mathf.Max(1, sfxMaxVoices);
        }

        void LimiterOnStart(string key, float now)
        {
            nextAllowed[key] = now + Mathf.Max(0f, sfxMinIntervalSec);
            voices[key] = (voices.TryGetValue(key, out int activeVoices) ? activeVoices : 0) + 1;
        }

        IEnumerator ReleaseVoiceAfter(string key, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (voices.TryGetValue(key, out int activeVoices))
            {
                voices[key] = Mathf.Max(0, activeVoices - 1);
            }
        }
    }
}
