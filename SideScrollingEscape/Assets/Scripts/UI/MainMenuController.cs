using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using UnityEngine;
using UnityEngine.UI;

// Trigger recompile
namespace SideScrollingEscape.UI
{
    /// <summary>
    /// Controls the main menu scene: starting a new game, adjusting volume and
    /// quitting.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Options")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        private void Start()
        {
            Time.timeScale = 1f;
            ShowMain();
            InitSliders();
        }

        private void InitSliders()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(AudioManager.MasterParam));
                masterVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetVolume(AudioManager.MasterParam, v));
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(AudioManager.SfxParam));
                sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetVolume(AudioManager.SfxParam, v));
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(AudioManager.MusicParam));
                musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SetVolume(AudioManager.MusicParam, v));
            }
        }

        /// <summary>Starts a new game from the first level.</summary>
        public void OnStartButton()
        {
            GameManager.Instance?.StartGame();
        }

        /// <summary>Shows the options panel.</summary>
        public void OnOptionsButton()
        {
            SetActive(mainPanel, false);
            SetActive(optionsPanel, true);
        }

        /// <summary>Returns to the main panel from options.</summary>
        public void OnBackButton()
        {
            ShowMain();
        }

        /// <summary>Quits the game.</summary>
        public void OnQuitButton()
        {
            GameManager.Instance?.QuitGame();
        }

        private void ShowMain()
        {
            SetActive(mainPanel, true);
            SetActive(optionsPanel, false);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
