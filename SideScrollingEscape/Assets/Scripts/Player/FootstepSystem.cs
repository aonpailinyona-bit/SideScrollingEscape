using SideScrollingEscape.Audio;
using SideScrollingEscape.Enemy;
using UnityEngine;

namespace SideScrollingEscape.Player
{
    /// <summary>
    /// Plays footstep sound effects cadenced to the player's movement. Sprinting
    /// produces faster, louder steps. Purely aud/visual; the killer reads the
    /// player's movement state directly for its hearing logic.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class FootstepSystem : MonoBehaviour
    {
        [Header("Clips")]
        [Tooltip("Footstep clips chosen at random for variation.")]
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Cadence")]
        [Tooltip("Seconds between steps while walking.")]
        [SerializeField] private float walkStepInterval = 0.5f;
        [Tooltip("Seconds between steps while sprinting.")]
        [SerializeField] private float sprintStepInterval = 0.3f;

        [Header("Volume")]
        [SerializeField] private float walkVolume = 0.4f;
        [SerializeField] private float sprintVolume = 0.7f;
        [Tooltip("Random pitch range applied to each step.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

        private PlayerController controller;
        private HidingSystem hiding;
        private float stepTimer;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            hiding = GetComponent<HidingSystem>();
        }

        private void Update()
        {
            bool hidden = hiding != null && hiding.IsHidden;

            if (!controller.IsMoving || hidden)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayStep();
                stepTimer = controller.IsSprinting ? sprintStepInterval : walkStepInterval;
            }
        }

        private void PlayStep()
        {
            NoiseType noiseType = controller.IsSprinting ? NoiseType.Sprint : NoiseType.Footstep;
            float loudness = controller.IsSprinting ? 1.25f : 0.65f;
            foreach (KillerAI killer in FindObjectsByType<KillerAI>())
                killer.HearNoise(transform.position, loudness, noiseType);

            if (footstepClips == null || footstepClips.Length == 0 || AudioManager.Instance == null)
            {
                return;
            }

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            float volume = controller.IsSprinting ? sprintVolume : walkVolume;
            float pitch = Random.Range(pitchRange.x, pitchRange.y);
            AudioManager.Instance.PlaySfxAt(clip, transform.position, volume, pitch);
        }
    }
}
