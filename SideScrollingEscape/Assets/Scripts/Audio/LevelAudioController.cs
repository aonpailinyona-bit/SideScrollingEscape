using SideScrollingEscape.Enemy;
using SideScrollingEscape.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.Audio
{
    /// <summary>
    /// Drives per-level music and ambience through the <see cref="AudioManager"/>:
    /// loops an ambient bed and crossfades to tense chase music while the killer
    /// is actively hunting the player.
    /// </summary>
    public class LevelAudioController : MonoBehaviour
    {
        [Tooltip("Looping ambient bed for the level.")]
        [SerializeField] private AudioClip ambientClip;

        [Tooltip("Looping music played while the killer is hunting.")]
        [SerializeField] private AudioClip chaseClip;

        [SerializeField] private float ambientVolume = 0.7f;
        [SerializeField] private float chaseVolume = 0.8f;

        private TensionDirector director;
        private bool chaseActive;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAmbient(ambientClip, ambientVolume);
            }

            director = TensionDirector.Instance != null ? TensionDirector.Instance : FindAnyObjectByType<TensionDirector>();
            if (director != null)
            {
                director.OnStateChanged += HandleTensionState;
                HandleTensionState(director.State);
            }
        }

        private void OnDestroy()
        {
            if (director != null)
            {
                director.OnStateChanged -= HandleTensionState;
            }
        }

        public void HandleTensionState(TensionState state)
        {
            bool hunting = state == TensionState.Hunted || state == TensionState.Escaped;
            if (hunting == chaseActive || AudioManager.Instance == null)
            {
                return;
            }

            chaseActive = hunting;
            AudioManager.Instance.PlayMusic(hunting ? chaseClip : null, chaseVolume);
        }
    }

    public enum TensionState
    {
        Calm,
        Suspicious,
        Hunted,
        Escaped,
        Cooldown
    }

    /// <summary>Combines every killer FSM into one stable tension state for audio and difficulty.</summary>
    public sealed class TensionDirector : MonoBehaviour
    {
        public static TensionDirector Instance { get; private set; }
        public TensionState State { get; private set; } = TensionState.Calm;
        public float Intensity01 { get; private set; }
        public event System.Action<TensionState> OnStateChanged;

        private KillerAI[] killers;
        private float stateTimer;
        private float huntedTimer;
        private float refreshTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool gameplay = scene.name == "Level1" || scene.name == "Level2" || scene.name == "Level3";
            if (!gameplay || FindAnyObjectByType<TensionDirector>() != null) return;
            new GameObject("TensionDirector").AddComponent<TensionDirector>();
        }

        private void Awake() => Instance = this;

        private void Start()
        {
            killers = FindObjectsByType<KillerAI>();
            foreach (LevelAudioController audio in FindObjectsByType<LevelAudioController>())
                audio.HandleTensionState(State);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ApplySpeedMultiplier(1f);
        }

        private void Update()
        {
            bool playing = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;
            if (!playing) return;

            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.1f;
            if (killers == null || killers.Length == 0) killers = FindObjectsByType<KillerAI>();

            bool hunting = false;
            bool suspicious = false;
            float strongestSuspicion = 0f;
            foreach (KillerAI killer in killers)
            {
                if (killer == null) continue;
                hunting |= killer.IsHunting || killer.State == KillerState.Scream;
                suspicious |= killer.State == KillerState.Alert || killer.State == KillerState.Listening ||
                    killer.State == KillerState.Investigate || killer.State == KillerState.InvestigateSound ||
                    killer.State == KillerState.ConfirmSound || killer.State == KillerState.Search;
                strongestSuspicion = Mathf.Max(strongestSuspicion, killer.Suspicion01);
            }

            TickState(hunting, suspicious || strongestSuspicion >= 0.28f);
            float targetIntensity = State == TensionState.Hunted ? 1f :
                State == TensionState.Escaped ? 0.72f :
                State == TensionState.Suspicious ? 0.48f :
                State == TensionState.Cooldown ? 0.2f : 0f;
            Intensity01 = Mathf.MoveTowards(Intensity01, targetIntensity, 0.09f);

            if (State == TensionState.Hunted) huntedTimer += 0.1f;
            else huntedTimer = 0f;
            ApplySpeedMultiplier(State == TensionState.Hunted
                ? Mathf.Lerp(1.08f, 1.01f, Mathf.InverseLerp(8f, 18f, huntedTimer)) : 1f);
        }

        private void TickState(bool hunting, bool suspicious)
        {
            stateTimer += 0.1f;
            if (hunting) { SetState(TensionState.Hunted); return; }

            switch (State)
            {
                case TensionState.Hunted:
                    SetState(TensionState.Escaped);
                    break;
                case TensionState.Escaped:
                    if (stateTimer >= 1.5f) SetState(TensionState.Cooldown);
                    break;
                case TensionState.Suspicious:
                    if (!suspicious && stateTimer >= 0.8f) SetState(TensionState.Cooldown);
                    break;
                case TensionState.Cooldown:
                    if (suspicious) SetState(TensionState.Suspicious);
                    else if (stateTimer >= 2.2f) SetState(TensionState.Calm);
                    break;
                default:
                    if (suspicious) SetState(TensionState.Suspicious);
                    break;
            }
        }

        private void ApplySpeedMultiplier(float multiplier)
        {
            if (killers == null) return;
            foreach (KillerAI killer in killers) killer?.SetDirectorChaseMultiplier(multiplier);
        }

        private void SetState(TensionState newState)
        {
            if (State == newState) return;
            State = newState;
            stateTimer = 0f;
            OnStateChanged?.Invoke(newState);
        }
    }
}
