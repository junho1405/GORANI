using System.Collections;
using UnityEngine;

namespace VN
{
    [DisallowMultipleComponent]
    public class VNAudio : MonoBehaviour
    {
        public static VNAudio Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource bgmSource;  // loop BGM
        public AudioSource sfxSource;  // one-shot SFX

        [Header("Defaults")]
        [Range(0f, 1f)] public float defaultBgmVolume = 0.8f;
        [Range(0f, 1f)] public float defaultSfxVolume = 1.0f;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!bgmSource)
            {
                var go = new GameObject("BGM_Source");
                go.transform.SetParent(transform);
                bgmSource = go.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (!sfxSource)
            {
                var go = new GameObject("SFX_Source");
                go.transform.SetParent(transform);
                sfxSource = go.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        public void PlayBgm(string resourcesPath, float volume = -1f, float fadeSeconds = 0.5f, bool loop = true)
        {
            var clip = Resources.Load<AudioClip>(resourcesPath);
            if (!clip) { Debug.LogWarning($"[VN] BGM clip not found: {resourcesPath}"); return; }

            bgmSource.loop = loop;
            if (fadeSeconds > 0f && bgmSource.isPlaying)
            {
                StopAllCoroutines();
                StartCoroutine(SwapBgmCo(clip, volume < 0 ? defaultBgmVolume : volume, fadeSeconds));
            }
            else
            {
                bgmSource.clip = clip;
                bgmSource.volume = (volume < 0 ? defaultBgmVolume : volume);
                bgmSource.Play();
            }
        }

        public void StopBgm(float fadeSeconds = 0.5f)
        {
            if (!bgmSource.isPlaying) return;
            if (fadeSeconds <= 0f) { bgmSource.Stop(); return; }
            StopAllCoroutines();
            StartCoroutine(FadeOutCo(bgmSource, fadeSeconds));
        }

        public void PlaySfx(string resourcesPath, float volume = -1f, float pitch = 1f)
        {
            var clip = Resources.Load<AudioClip>(resourcesPath);
            if (!clip) { Debug.LogWarning($"[VN] SFX clip not found: {resourcesPath}"); return; }
            var vol = (volume < 0 ? defaultSfxVolume : Mathf.Clamp01(volume));
            var prevPitch = sfxSource.pitch;
            sfxSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            sfxSource.PlayOneShot(clip, vol);
            sfxSource.pitch = prevPitch;
        }

        IEnumerator SwapBgmCo(AudioClip next, float targetVol, float fade)
        {
            yield return FadeOutCo(bgmSource, fade);
            bgmSource.clip = next;
            bgmSource.volume = 0f;
            bgmSource.Play();
            yield return FadeInCo(bgmSource, targetVol, fade);
        }

        static IEnumerator FadeOutCo(AudioSource src, float t)
        {
            float start = src.volume;
            float elapsed = 0f;
            while (elapsed < t)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, 0f, elapsed / t);
                yield return null;
            }
            src.Stop();
            src.volume = start;
        }

        static IEnumerator FadeInCo(AudioSource src, float target, float t)
        {
            float elapsed = 0f;
            src.volume = 0f;
            while (elapsed < t)
            {
                elapsed += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(0f, target, elapsed / t);
                yield return null;
            }
            src.volume = target;
        }
    }
}
