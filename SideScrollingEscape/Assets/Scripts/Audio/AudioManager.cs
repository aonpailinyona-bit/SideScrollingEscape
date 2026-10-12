using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.Audio
{
    /// <summary>
    /// Persistent singleton that plays pooled one-shot sound effects and controls
    /// the project <see cref="AudioMixer"/> volumes through exposed parameters.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class AudioManager : MonoBehaviour
    {
        /// <summary>Exposed mixer parameter names.</summary>
        public const string MasterParam = "MasterVolume";
        public const string SfxParam = "SFXVolume";
        public const string MusicParam = "MusicVolume";
        public const string AmbientParam = "AmbientVolume";
        public const string UiParam = "UIVolume";

        [Header("Mixer")]
        [Tooltip("Project audio mixer asset.")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup uiGroup;

        [Header("SFX Pool")]
        [Tooltip("Number of pooled AudioSources for one-shot SFX.")]
        [SerializeField] private int sfxVoiceCount = 12;

        private readonly List<AudioSource> sfxVoices = new List<AudioSource>();
        private int nextVoice;
        private AudioSource musicSource;
        private AudioSource ambientSource;
        private float musicRequestedVolume = 1f;
        private float ambientRequestedVolume = 1f;
        private float backgroundVolumeMultiplier = 1f;

        /// <summary>Singleton instance.</summary>
        public static AudioManager Instance { get; private set; }

        /// <summary>
        /// Ensures an AudioManager exists when entering play mode on any scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPools();
            ApplySavedVolumes();

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Stop everything when a scene unloads to prevent sound leakage
            StopAll();
            SetBackgroundVolumeMultiplier(1f);
        }

        private void BuildPools()
        {
            for (int i = 0; i < sfxVoiceCount; i++)
            {
                var go = new GameObject("SFXVoice_" + i);
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.outputAudioMixerGroup = sfxGroup;
                src.spatialBlend = 0f;
                sfxVoices.Add(src);
            }

            var musicGo = new GameObject("MusicSource");
            musicGo.transform.SetParent(transform);
            musicSource = musicGo.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.outputAudioMixerGroup = musicGroup;

            var ambientGo = new GameObject("AmbientSource");
            ambientGo.transform.SetParent(transform);
            ambientSource = ambientGo.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.outputAudioMixerGroup = ambientGroup;
        }

        /// <summary>
        /// Stops all active audio sources (SFX, Music, Ambient).
        /// </summary>
        public void StopAll()
        {
            StopAllSFX();
            musicSource?.Stop();
            ambientSource?.Stop();
        }

        /// <summary>
        /// Stops all pooled SFX voices.
        /// </summary>
        public void StopAllSFX()
        {
            foreach (var src in sfxVoices)
            {
                src.Stop();
            }
        }

        /// <summary>
        /// Plays a 2D one-shot sound effect through the SFX group.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="volume">Linear volume scale (0..1).</param>
        /// <param name="pitch">Playback pitch.</param>
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sfxVoices.Count == 0)
            {
                return;
            }

            AudioSource src = sfxVoices[nextVoice];
            nextVoice = (nextVoice + 1) % sfxVoices.Count;
            src.transform.position = Vector3.zero;
            src.spatialBlend = 0f;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Plays a positional one-shot sound effect through the SFX group.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="position">World position of the sound.</param>
        /// <param name="volume">Linear volume scale (0..1).</param>
        /// <param name="pitch">Playback pitch.</param>
        public void PlaySfxAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sfxVoices.Count == 0)
            {
                return;
            }

            AudioSource src = sfxVoices[nextVoice];
            nextVoice = (nextVoice + 1) % sfxVoices.Count;
            src.transform.position = position;
            src.spatialBlend = 0.8f;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Starts looping ambient audio (replacing any current ambient clip).
        /// </summary>
        /// <param name="clip">Ambient clip to loop.</param>
        /// <param name="volume">Linear volume (0..1).</param>
        public void PlayAmbient(AudioClip clip, float volume = 1f)
        {
            if (ambientSource == null)
            {
                return;
            }

            ambientRequestedVolume = volume;
            ambientSource.clip = clip;
            ambientSource.volume = volume * backgroundVolumeMultiplier;
            if (clip != null)
            {
                ambientSource.Play();
            }
            else
            {
                ambientSource.Stop();
            }
        }

        /// <summary>
        /// Starts looping music (replacing any current track).
        /// </summary>
        /// <param name="clip">Music clip to loop.</param>
        /// <param name="volume">Linear volume (0..1).</param>
        public void PlayMusic(AudioClip clip, float volume = 1f)
        {
            if (musicSource == null)
            {
                return;
            }

            musicRequestedVolume = volume;
            musicSource.clip = clip;
            musicSource.volume = volume * backgroundVolumeMultiplier;
            if (clip != null)
            {
                musicSource.Play();
            }
            else
            {
                musicSource.Stop();
            }
        }

        /// <summary>Temporarily scales only music and ambience for the active level.</summary>
        public void SetBackgroundVolumeMultiplier(float multiplier)
        {
            backgroundVolumeMultiplier = Mathf.Clamp01(multiplier);
            if (musicSource != null)
                musicSource.volume = musicRequestedVolume * backgroundVolumeMultiplier;
            if (ambientSource != null)
                ambientSource.volume = ambientRequestedVolume * backgroundVolumeMultiplier;
        }

        /// <summary>
        /// Sets a mixer group volume from a normalized 0..1 value, saving it to PlayerPrefs.
        /// </summary>
        /// <param name="exposedParam">Exposed mixer parameter name.</param>
        /// <param name="normalized">Linear volume from 0 (silent) to 1 (full).</param>
        public void SetVolume(string exposedParam, float normalized)
        {
            if (mixer == null || string.IsNullOrEmpty(exposedParam))
            {
                return;
            }

            normalized = Mathf.Clamp01(normalized);
            float dB = normalized <= 0.0001f ? -80f : Mathf.Log10(normalized) * 20f;
            mixer.SetFloat(exposedParam, dB);
            PlayerPrefs.SetFloat("vol_" + exposedParam, normalized);

            // The normal level soundtrack is routed through Ambient, while
            // chase music is routed through Music. One Music slider should
            // control both so it behaves consistently in menus and levels.
            if (exposedParam == MusicParam)
            {
                mixer.SetFloat(AmbientParam, dB);
                PlayerPrefs.SetFloat("vol_" + AmbientParam, normalized);
            }
        }

        /// <summary>
        /// Returns the saved normalized volume for an exposed parameter (defaults to 1).
        /// </summary>
        /// <param name="exposedParam">Exposed mixer parameter name.</param>
        /// <returns>Normalized volume 0..1.</returns>
        public float GetVolume(string exposedParam)
        {
            return PlayerPrefs.GetFloat("vol_" + exposedParam, 1f);
        }

        private void ApplySavedVolumes()
        {
            SetVolume(MasterParam, GetVolume(MasterParam));
            SetVolume(SfxParam, GetVolume(SfxParam));
            SetVolume(AmbientParam, GetVolume(AmbientParam));
            SetVolume(MusicParam, GetVolume(MusicParam));
            SetVolume(UiParam, GetVolume(UiParam));
        }
    }
}
