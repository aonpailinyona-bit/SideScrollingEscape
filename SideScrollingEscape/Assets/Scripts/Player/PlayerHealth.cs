using System;
using System.Collections;
using System.Collections.Generic;
using SideScrollingEscape.Core;
using SideScrollingEscape.Data;
using UnityEngine;

namespace SideScrollingEscape.Player
{
    /// <summary>
    /// Tracks the player's health, applies damage and healing, and notifies the
    /// <see cref="GameManager"/> when the player dies.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Fallback max health used when no LevelConfig is available.")]
        [SerializeField] private float fallbackMaxHealth = 100f;

        [Tooltip("Seconds of invulnerability after taking damage.")]
        [SerializeField] private float invulnerabilityDuration = 1.5f;


        /// <summary>Raised when health changes (current, max).</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Raised when the player takes damage (damage amount).</summary>
        public event Action<float> OnDamaged;

        /// <summary>Raised once when the player dies.</summary>
        public event Action OnDied;

        /// <summary>Current health value.</summary>
        public float CurrentHealth { get; private set; }

        /// <summary>Maximum health value.</summary>
        public float MaxHealth { get; private set; }

        /// <summary>True once health has reached zero.</summary>
        public bool IsDead { get; private set; }

        /// <summary>True while the player is invincible (e.g. after taking damage).</summary>
        public bool IsInvincible { get; private set; }

        private void Awake()
        {
            LevelConfig config = GameManager.Instance != null ? GameManager.Instance.CurrentLevelConfig : null;
            MaxHealth = config != null ? config.PlayerMaxHealth : fallbackMaxHealth;
            CurrentHealth = MaxHealth;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Applies damage to the player. Ignored while dead or invincible.
        /// </summary>
        /// <param name="amount">Amount of damage to apply (positive value).</param>
        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f || IsInvincible)
            {
                return;
            }

            // Block damage entirely if the player is hidden
            var hiding = GetComponent<HidingSystem>();
            if (hiding != null && hiding.IsHidden)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            PlayerController controller = GetComponent<PlayerController>();
            controller?.EnterHurtState();
            OnDamaged?.Invoke(amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvincibilityCoroutine());
            }
        }

        private IEnumerator InvincibilityCoroutine()
        {
            IsInvincible = true;

            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            var playerColliders = GetComponentsInChildren<Collider2D>();
            var hiding = GetComponent<HidingSystem>();

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
                        Physics2D.IgnoreCollision(pCol, kCol, true);
                    }
                }
            }

            float timer = 0f;
            float flashInterval = 0.1f;
            while (timer < invulnerabilityDuration)
            {
                if (sprite != null)
                {
                    if (hiding != null && hiding.IsHidden)
                    {
                        sprite.enabled = false;
                    }
                    else
                    {
                        sprite.enabled = !sprite.enabled;
                    }
                }
                yield return new WaitForSeconds(flashInterval);
                timer += flashInterval;
            }

            if (sprite != null)
            {
                sprite.enabled = hiding == null || !hiding.IsHidden;
            }

            foreach (var pCol in playerColliders)
            {
                foreach (var kCol in killerColliders)
                {
                    if (pCol != null && kCol != null)
                    {
                        // KillerAI attacks by distance; keep body collision off
                        // permanently so invincibility ending cannot reintroduce
                        // the head-standing/stuck AI bug.
                        Physics2D.IgnoreCollision(pCol, kCol, true);
                    }
                }
            }

            IsInvincible = false;
            GetComponent<PlayerController>()?.ExitHurtState();
        }

        /// <summary>
        /// Restores health up to the maximum.
        /// </summary>
        /// <param name="amount">Amount of health to restore (positive value).</param>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Immediately kills the player.
        /// </summary>
        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Die();
        }

        private void Die()
        {
            IsDead = true;
            GetComponent<PlayerController>()?.EnterDeadState();
            OnDied?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyPlayerDied();
            }
        }
    }
}
