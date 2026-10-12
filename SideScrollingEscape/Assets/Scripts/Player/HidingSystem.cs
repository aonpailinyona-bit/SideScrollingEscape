using System;
using System.Collections.Generic;
using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using UnityEngine;

namespace SideScrollingEscape.Player
{
    /// <summary>
    /// Lets the player hide inside designated hiding spots. While hidden the
    /// player cannot move and is not detectable by the killer.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class HidingSystem : MonoBehaviour
    {
        [Tooltip("Tag identifying hiding-spot trigger zones.")]
        [SerializeField] private string hidingSpotTag = "HidingSpot";

        [Tooltip("Turn the flashlight off automatically when hiding.")]
        [SerializeField] private bool flashlightOffWhileHidden = true;

        [Header("Audio")]
        [SerializeField] private AudioClip hideClip;

        private PlayerController controller;
        private FlashlightController flashlight;
        private SpriteRenderer spriteRenderer;
        private PlayerHealth playerHealth;
        private int spotsInRange;

        /// <summary>Raised when the hidden state changes.</summary>
        public event Action<bool> OnHiddenChanged;

        /// <summary>True while the player is currently hidden.</summary>
        public bool IsHidden { get; private set; }

        /// <summary>True while the player is standing inside a hiding spot.</summary>
        public bool CanHide => spotsInRange > 0 && controller != null && !controller.IsFrozen;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            flashlight = GetComponent<FlashlightController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            controller.HideToggled += ToggleHide;
            controller.OnFrozenStateChanged += HandleFrozenStateChanged;
        }

        private void OnDisable()
        {
            controller.HideToggled -= ToggleHide;
            controller.OnFrozenStateChanged -= HandleFrozenStateChanged;
        }

        /// <summary>
        /// Toggles hiding when inside a hiding spot, or exits hiding from anywhere.
        /// </summary>
        public void ToggleHide()
        {
            if (controller == null || controller.IsFrozen) return;

            if (IsHidden)
            {
                SetHidden(false);
            }
            else if (CanHide)
            {
                SetHidden(true);
            }
        }

        /// <summary>Forces the player out when a killer successfully searches the occupied spot.</summary>
        public void ForceReveal()
        {
            if (IsHidden) SetHidden(false);
        }

        private void HandleFrozenStateChanged(bool frozen)
        {
            // Being petrified cannot be used to enter or remain invulnerable in a hiding spot.
            if (frozen && IsHidden)
            {
                SetHidden(false);
                controller.SetHiddenState(false);
            }
        }

        private void SetHidden(bool hidden)
        {
            if (IsHidden == hidden)
            {
                return;
            }

            IsHidden = hidden;
            controller.SetHiddenState(hidden);

            if (hidden && flashlightOffWhileHidden && flashlight != null)
            {
                flashlight.SetOn(false, playSound: false);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !hidden;
            }

            var playerColliders = GetComponentsInChildren<Collider2D>();
            var killerComponents = UnityEngine.Object.FindObjectsByType<MonoBehaviour>();
            List<Collider2D> killerColliders = new List<Collider2D>();
            foreach (var mono in killerComponents)
            {
                if (mono != null && mono.GetType().Name == "KillerAI")
                {
                    killerColliders.AddRange(mono.GetComponentsInChildren<Collider2D>());
                }
            }

            foreach (var pCol in playerColliders)
            {
                foreach (var kCol in killerColliders)
                {
                    if (pCol != null && kCol != null)
                    {
                        // Never restore physical player/killer collision when
                        // leaving a hiding spot; attacks are distance based.
                        Physics2D.IgnoreCollision(pCol, kCol, true);
                    }
                }
            }

            if (hideClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(hideClip, 0.6f);
            }

            OnHiddenChanged?.Invoke(hidden);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(hidingSpotTag))
            {
                spotsInRange++;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(hidingSpotTag))
            {
                spotsInRange = Mathf.Max(0, spotsInRange - 1);
                if (spotsInRange == 0 && IsHidden)
                {
                    SetHidden(false);
                }
            }
        }
    }
}
