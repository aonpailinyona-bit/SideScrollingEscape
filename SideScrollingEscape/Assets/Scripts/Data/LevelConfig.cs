using UnityEngine;

namespace SideScrollingEscape.Data
{
    /// <summary>
    /// ScriptableObject holding all tunable configuration for a single level,
    /// including the scene to load, the number of keys required to escape, and
    /// difficulty parameters for the player, flashlight and killer.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "SideScrollingEscape/Level Config", order = 1)]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name of the level shown in UI.")]
        [SerializeField] private string levelName = "Level 1";

        [Tooltip("Name of the scene (must be added to Build Settings) that this level loads.")]
        [SerializeField] private string sceneName = "Level1";

        [Tooltip("Short difficulty descriptor, e.g. 'Easy', 'Normal', 'Nightmare'.")]
        [SerializeField] private string difficultyLabel = "Easy";

        [Header("Escape Objective")]
        [Tooltip("Number of key items the player must collect to unlock the exit.")]
        [SerializeField] private int requiredKeyCount = 5;

        [Header("Player")]
        [Tooltip("Maximum player health for this level.")]
        [SerializeField] private float playerMaxHealth = 100f;

        [Tooltip("Base walking speed of the player.")]
        [SerializeField] private float playerMoveSpeed = 4.5f;

        [Tooltip("Sprint speed of the player (noisier, attracts the killer).")]
        [SerializeField] private float playerSprintSpeed = 7.5f;

        [Header("Flashlight")]
        [Tooltip("Maximum flashlight battery (seconds of use).")]
        [SerializeField] private float flashlightMaxBattery = 60f;

        [Tooltip("Battery drained per second while the flashlight is on.")]
        [SerializeField] private float flashlightDrainPerSecond = 1f;

        [Header("Killer AI")]
        [Tooltip("Killer patrol movement speed.")]
        [SerializeField] private float killerPatrolSpeed = 2.5f;

        [Tooltip("Killer chase movement speed.")]
        [SerializeField] private float killerChaseSpeed = 5.5f;

        [Tooltip("How far the killer can see the player (world units).")]
        [SerializeField] private float killerSightRange = 8f;

        [Tooltip("How far the killer can hear footsteps/sprinting (world units).")]
        [SerializeField] private float killerHearingRange = 6f;

        [Tooltip("Range at which the killer can land an attack.")]
        [SerializeField] private float killerAttackRange = 1.2f;

        [Tooltip("Damage dealt per attack.")]
        [SerializeField] private float killerAttackDamage = 35f;

        [Tooltip("Seconds between consecutive killer attacks.")]
        [SerializeField] private float killerAttackCooldown = 1.5f;

        [Tooltip("Seconds the killer keeps searching after losing sight of the player.")]
        [SerializeField] private float killerSearchDuration = 5f;

        // -- Identity --------------------------------------------------------
        /// <summary>Display name of the level shown in UI.</summary>
        public string LevelName => levelName;
        /// <summary>Name of the scene that this level loads.</summary>
        public string SceneName => sceneName;
        /// <summary>Short difficulty descriptor.</summary>
        public string DifficultyLabel => difficultyLabel;

        // -- Objective -------------------------------------------------------
        /// <summary>Number of key items required to unlock the exit.</summary>
        public int RequiredKeyCount => requiredKeyCount;

        // -- Player ----------------------------------------------------------
        /// <summary>Maximum player health for this level.</summary>
        public float PlayerMaxHealth => playerMaxHealth;
        /// <summary>Base walking speed of the player.</summary>
        public float PlayerMoveSpeed => playerMoveSpeed;
        /// <summary>Sprint speed of the player.</summary>
        public float PlayerSprintSpeed => playerSprintSpeed;

        // -- Flashlight ------------------------------------------------------
        /// <summary>Maximum flashlight battery in seconds.</summary>
        public float FlashlightMaxBattery => flashlightMaxBattery;
        /// <summary>Battery drained per second while on.</summary>
        public float FlashlightDrainPerSecond => flashlightDrainPerSecond;

        // -- Killer ----------------------------------------------------------
        /// <summary>Killer patrol movement speed.</summary>
        public float KillerPatrolSpeed => killerPatrolSpeed;
        /// <summary>Killer chase movement speed.</summary>
        public float KillerChaseSpeed => killerChaseSpeed;
        /// <summary>How far the killer can see the player.</summary>
        public float KillerSightRange => killerSightRange;
        /// <summary>How far the killer can hear the player.</summary>
        public float KillerHearingRange => killerHearingRange;
        /// <summary>Range at which the killer can attack.</summary>
        public float KillerAttackRange => killerAttackRange;
        /// <summary>Damage dealt per attack.</summary>
        public float KillerAttackDamage => killerAttackDamage;
        /// <summary>Seconds between consecutive attacks.</summary>
        public float KillerAttackCooldown => killerAttackCooldown;
        /// <summary>Seconds the killer keeps searching after losing the player.</summary>
        public float KillerSearchDuration => killerSearchDuration;
    }
}
