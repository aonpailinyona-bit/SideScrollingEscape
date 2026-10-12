using System;
using System.Collections.Generic;
using SideScrollingEscape.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.Core
{
    /// <summary>
    /// High level states the game can be in.
    /// </summary>
    public enum GameState
    {
        /// <summary>Front-end menu, no gameplay running.</summary>
        MainMenu,
        /// <summary>Gameplay is active and time is running.</summary>
        Playing,
        /// <summary>Gameplay is suspended; time scale is 0.</summary>
        Paused,
        /// <summary>The player has died.</summary>
        Dead,
        /// <summary>The current level has been completed.</summary>
        LevelComplete,
        /// <summary>All levels have been completed.</summary>
        GameWon
    }

    /// <summary>
    /// Persistent singleton responsible for global game state, level progression
    /// and scene loading. Other systems read <see cref="CurrentLevelConfig"/> for
    /// their difficulty tuning and subscribe to <see cref="OnGameStateChanged"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [Header("Levels (in play order)")]
        [Tooltip("Level configurations played in order. Index 0 is the first level.")]
        [SerializeField] private List<LevelConfig> levels = new List<LevelConfig>();

        /// <summary>Singleton instance.</summary>
        public static GameManager Instance { get; private set; }

        /// <summary>Raised whenever <see cref="CurrentState"/> changes.</summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>Raised after a level scene has finished loading, passing its config.</summary>
        public event Action<LevelConfig> OnLevelLoaded;

        /// <summary>The current high level game state.</summary>
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        /// <summary>Index of the currently active level within <see cref="levels"/>.</summary>
        public int CurrentLevelIndex { get; private set; }

        /// <summary>Total number of configured levels.</summary>
        public int LevelCount => levels.Count;

        /// <summary>Configuration for the currently active level (null in the menu).</summary>
        public LevelConfig CurrentLevelConfig =>
            (CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Count) ? levels[CurrentLevelIndex] : null;

        /// <summary>
        /// Ensures a GameManager exists when entering play mode directly on a
        /// gameplay scene during development.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            GameManager prefab = Resources.Load<GameManager>("GameManager");
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
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            // sceneLoaded does not fire for the scene that is already open when we
            // enter play mode, so evaluate the active scene once on startup. This
            // makes pressing Play directly on a level scene work as expected.
            EvaluateInitialScene();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                Instance = null;
            }
        }

        private void EvaluateInitialScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.name == MainMenuSceneName)
            {
                SetState(GameState.MainMenu);
                return;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null && levels[i].SceneName == active.name)
                {
                    CurrentLevelIndex = i;
                    Time.timeScale = 1f;
                    SetState(GameState.Playing);
                    OnLevelLoaded?.Invoke(levels[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Starts a new game from the first level.
        /// </summary>
        public void StartGame()
        {
            CurrentLevelIndex = 0;
            LoadCurrentLevel();
        }

        /// <summary>
        /// Loads the level at the given index (clamped to the configured range).
        /// </summary>
        /// <param name="index">Zero based level index to load.</param>
        public void LoadLevel(int index)
        {
            CurrentLevelIndex = Mathf.Clamp(index, 0, Mathf.Max(0, levels.Count - 1));
            LoadCurrentLevel();
        }

        /// <summary>
        /// Reloads the current level scene (used after the player dies).
        /// </summary>
        public void RestartLevel()
        {
            LoadCurrentLevel();
        }

        /// <summary>
        /// Advances to the next level, or to the win state if this was the last level.
        /// </summary>
        public void ContinueToNextLevel()
        {
            if (CurrentLevelIndex + 1 < levels.Count)
            {
                CurrentLevelIndex++;
                LoadCurrentLevel();
            }
            else
            {
                SetState(GameState.GameWon);
            }
        }

        /// <summary>
        /// Loads the main menu scene and resets state.
        /// </summary>
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SetState(GameState.MainMenu);
            SceneManager.LoadScene(MainMenuSceneName);
        }

        /// <summary>
        /// Toggles between <see cref="GameState.Playing"/> and <see cref="GameState.Paused"/>.
        /// Has no effect outside of active gameplay.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        /// <summary>Suspends gameplay and freezes time.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        /// <summary>Resumes gameplay and restores time.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Called when the player's health reaches zero. Freezes gameplay and
        /// transitions to the dead state.
        /// </summary>
        public void NotifyPlayerDied()
        {
            if (CurrentState == GameState.Dead)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Dead);
        }

        /// <summary>
        /// Called when the player reaches the exit with all keys collected.
        /// </summary>
        public void NotifyLevelCompleted()
        {
            if (CurrentState == GameState.LevelComplete)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.LevelComplete);
        }

        /// <summary>
        /// Quits the application (or stops play mode in the editor).
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadCurrentLevel()
        {
            LevelConfig config = CurrentLevelConfig;
            if (config == null)
            {
                Debug.LogError("[GameManager] No LevelConfig assigned for index " + CurrentLevelIndex);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(config.SceneName);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MainMenuSceneName)
            {
                Time.timeScale = 1f;
                SetState(GameState.MainMenu);
                return;
            }

            LevelConfig config = CurrentLevelConfig;
            if (config != null && scene.name == config.SceneName)
            {
                Time.timeScale = 1f;
                SetState(GameState.Playing);
                OnLevelLoaded?.Invoke(config);
            }
        }

        /// <summary>
        /// Sets the current state and notifies listeners if it changed.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
    }
}
