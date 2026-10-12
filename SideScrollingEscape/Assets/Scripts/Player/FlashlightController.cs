using System;
using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using SideScrollingEscape.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SideScrollingEscape.Player
{
    /// <summary>
    /// Controls the player's flashlight: toggling a URP <see cref="Light2D"/>,
    /// draining a battery while on, and aiming the cone in the facing direction.
    /// While on, the flashlight makes the player easier for the killer to spot.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class FlashlightController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("URP 2D light used as the flashlight cone.")]
        [SerializeField] private Light2D flashlight;
        [Tooltip("Transform rotated to aim the cone (defaults to the light's transform).")]
        [SerializeField] private Transform aimPivot;

        [Header("Settings")]
        [SerializeField] private float fallbackMaxBattery = 60f;
        [SerializeField] private float fallbackDrainPerSecond = 1f;
        [Tooltip("Start the level with the flashlight switched on.")]
        [SerializeField] private bool startOn = true;

        [Header("Audio")]
        [SerializeField] private AudioClip toggleClip;

        private PlayerController controller;
        private float maxBattery;
        private float drainPerSecond;

        /// <summary>Raised when the flashlight turns on/off.</summary>
        public event Action<bool> OnStateChanged;

        /// <summary>Raised when battery changes (normalized 0..1).</summary>
        public event Action<float> OnBatteryChanged;

        /// <summary>True while the flashlight is emitting light.</summary>
        public bool IsOn { get; private set; }

        /// <summary>Remaining battery in seconds.</summary>
        public float Battery { get; private set; }

        /// <summary>Remaining battery as a 0..1 fraction.</summary>
        public float BatteryNormalized => maxBattery > 0f ? Battery / maxBattery : 0f;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            if (aimPivot == null && flashlight != null)
            {
                aimPivot = flashlight.transform;
            }

            // Apply requested settings
            if (flashlight != null)
            {
                flashlight.lightType = Light2D.LightType.Point;
                flashlight.pointLightInnerAngle = 15f;
                flashlight.pointLightOuterAngle = 30f;
                flashlight.intensity = 1.2f;
                flashlight.color = new Color(1f, 240f / 255f, 200f / 255f, 1f);
                
                // Initial setup for facing right
                flashlight.transform.localPosition = new Vector3(0.6f, 0f, 0f);
                flashlight.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            LevelConfig config = GameManager.Instance != null ? GameManager.Instance.CurrentLevelConfig : null;
            maxBattery = config != null ? config.FlashlightMaxBattery : fallbackMaxBattery;
            drainPerSecond = config != null ? config.FlashlightDrainPerSecond : fallbackDrainPerSecond;
            Battery = maxBattery;
        }

        private void OnEnable()
        {
            controller.FlashlightToggled += Toggle;
        }

        private void OnDisable()
        {
            controller.FlashlightToggled -= Toggle;
        }

        private void Start()
        {
            SetOn(startOn && Battery > 0f, playSound: false);
            OnBatteryChanged?.Invoke(BatteryNormalized);
        }

        private void Update()
        {
            if (IsOn)
            {
                Battery = Mathf.Max(0f, Battery - drainPerSecond * Time.deltaTime);
                OnBatteryChanged?.Invoke(BatteryNormalized);
                if (Battery <= 0f)
                {
                    SetOn(false, playSound: false);
                }
            }

            AimCone();
        }

        /// <summary>
        /// Toggles the flashlight on or off (no effect with an empty battery).
        /// </summary>
        public void Toggle()
        {
            var hiding = GetComponent<HidingSystem>();
            if (hiding != null && hiding.IsHidden)
            {
                return;
            }
            SetOn(!IsOn && Battery > 0f, playSound: true);
        }

        /// <summary>
        /// Explicitly sets the flashlight state.
        /// </summary>
        /// <param name="on">Desired on/off state.</param>
        /// <param name="playSound">Whether to play the toggle sound.</param>
        public void SetOn(bool on, bool playSound)
        {
            if (on)
            {
                var hiding = GetComponent<HidingSystem>();
                if (hiding != null && hiding.IsHidden)
                {
                    return;
                }
            }

            IsOn = on;
            if (flashlight != null)
            {
                flashlight.enabled = on;
            }

            if (playSound && toggleClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(toggleClip, 0.6f);
            }

            OnStateChanged?.Invoke(IsOn);
        }

        private void AimCone()
        {
            if (flashlight == null || controller == null)
            {
                return;
            }

            // Reset aimPivot rotation to identity to use direct light rotation/position flipping
            if (aimPivot != null && aimPivot != flashlight.transform)
            {
                aimPivot.localRotation = Quaternion.identity;
            }

            bool facingRight = controller.FacingDirection >= 0;
            float x = facingRight ? 0.6f : -0.6f;
            float z = facingRight ? 270f : 90f; 

            flashlight.transform.localPosition = new Vector3(x, 0f, 0f);
            flashlight.transform.localRotation = Quaternion.Euler(0f, 0f, z);
        }
    }
}
