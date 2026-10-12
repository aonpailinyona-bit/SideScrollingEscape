using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using SideScrollingEscape.Enemy;
using SideScrollingEscape.Player;
using UnityEngine;

namespace SideScrollingEscape.Items
{
    public enum ExitDoorState
    {
        Locked,
        Ready,
        Unlocking,
        Open,
        Transitioning,
        Completed
    }

    /// <summary>
    /// The level exit. The player can interact with it once all required keys
    /// have been collected, which completes the level.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        [Header("Visuals")]
        [Tooltip("Optional renderer tinted to show locked/unlocked state.")]
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Color lockedColor = new Color(0.6f, 0.2f, 0.2f);
        [SerializeField] private Color unlockedColor = new Color(0.3f, 0.8f, 0.4f);

        [Header("Audio")]
        [SerializeField] private AudioClip lockedClip;
        [SerializeField] private AudioClip escapeClip;

        [Header("Unlocking")]
        [SerializeField] private float unlockHoldDuration = 2f;
        [SerializeField] private float dangerCancelRadius = 3.2f;

        private bool unlockedNotified;
        private PlayerController activePlayer;
        private PlayerHealth activeHealth;
        private float unlockProgress;
        private bool interruptedByDamage;

        public ExitDoorState State { get; private set; } = ExitDoorState.Locked;
        public float UnlockProgress01 => unlockHoldDuration > 0f
            ? Mathf.Clamp01(unlockProgress / unlockHoldDuration) : 1f;
        public event System.Action<ExitDoorState> OnStateChanged;

        private void Start()
        {
            if (doorRenderer == null)
            {
                doorRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            UpdateVisual();
            SetState(HasAllKeys ? ExitDoorState.Ready : ExitDoorState.Locked);

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnKeyCountChanged += HandleKeyCountChanged;
            }
        }

        private void OnDestroy()
        {
            UnsubscribePlayer();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnKeyCountChanged -= HandleKeyCountChanged;
            }
        }

        private void HandleKeyCountChanged(int current, int required)
        {
            if (State == ExitDoorState.Completed || State == ExitDoorState.Transitioning) return;
            SetState(HasAllKeys ? ExitDoorState.Ready : ExitDoorState.Locked);
            UpdateVisual();
        }

        private void Update()
        {
            if (State != ExitDoorState.Unlocking || activePlayer == null) return;

            bool tooFar = Vector2.Distance(activePlayer.transform.position, transform.position) > 1.8f;
            bool dangerNear = false;
            foreach (KillerAI killer in FindObjectsByType<KillerAI>())
            {
                if (killer != null && Vector2.Distance(killer.transform.position, activePlayer.transform.position) <= dangerCancelRadius)
                { dangerNear = true; break; }
            }

            if (!activePlayer.IsInteractHeld || tooFar || dangerNear || interruptedByDamage)
            {
                CancelUnlock();
                return;
            }

            unlockProgress += Time.deltaTime;
            if (unlockProgress >= unlockHoldDuration) CompleteUnlock();
        }

        private bool HasAllKeys =>
            InventoryManager.Instance != null && InventoryManager.Instance.HasAllKeys;

        /// <inheritdoc />
        public bool CanInteract(GameObject interactor)
        {
            return State != ExitDoorState.Completed && State != ExitDoorState.Transitioning;
        }

        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (State == ExitDoorState.Ready && HasAllKeys)
            {
                activePlayer = interactor.GetComponent<PlayerController>();
                activeHealth = interactor.GetComponent<PlayerHealth>();
                if (activePlayer == null) return;
                interruptedByDamage = false;
                unlockProgress = 0f;
                activePlayer.MovementLocked = true;
                if (activeHealth != null) activeHealth.OnDamaged += HandlePlayerDamaged;
                SetState(ExitDoorState.Unlocking);
            }
            else if (State == ExitDoorState.Locked && lockedClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxAt(lockedClip, transform.position, 0.8f);
            }
        }

        /// <inheritdoc />
        public string GetPrompt()
        {
            if (State == ExitDoorState.Unlocking)
                return $"Hold E to unlock  {Mathf.RoundToInt(UnlockProgress01 * 100f)}%";
            if (State == ExitDoorState.Open)
                return "Door open";
            if (HasAllKeys)
            {
                return "Hold E to UNLOCK";
            }

            int remaining = InventoryManager.Instance != null
                ? Mathf.Max(0, InventoryManager.Instance.RequiredKeyCount - InventoryManager.Instance.KeyCount)
                : 0;
            return $"Locked - find {remaining} more key(s)";
        }

        private void UpdateVisual()
        {
            if (doorRenderer == null)
            {
                return;
            }

            bool open = HasAllKeys;
            doorRenderer.color = open ? unlockedColor : lockedColor;

            if (open && !unlockedNotified)
            {
                unlockedNotified = true;
            }
        }

        private void HandlePlayerDamaged(float amount) => interruptedByDamage = true;

        private void CancelUnlock()
        {
            if (activePlayer != null) activePlayer.MovementLocked = false;
            UnsubscribePlayer();
            unlockProgress = 0f;
            SetState(HasAllKeys ? ExitDoorState.Ready : ExitDoorState.Locked);
        }

        private void CompleteUnlock()
        {
            SetState(ExitDoorState.Open);
            if (escapeClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxAt(escapeClip, transform.position, 1f);

            SetState(ExitDoorState.Transitioning);
            if (activePlayer != null)
            {
                activePlayer.MovementLocked = false;
                activePlayer.BeginTransition();
            }
            UnsubscribePlayer();
            GameManager.Instance?.NotifyLevelCompleted();
            SetState(ExitDoorState.Completed);
        }

        private void UnsubscribePlayer()
        {
            if (activeHealth != null) activeHealth.OnDamaged -= HandlePlayerDamaged;
            activeHealth = null;
            activePlayer = null;
        }

        private void SetState(ExitDoorState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
            UpdateVisual();
        }
    }
}
