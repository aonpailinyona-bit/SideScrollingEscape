using SideScrollingEscape.Core;
using SideScrollingEscape.Player;
using UnityEngine;

namespace SideScrollingEscape.Items
{
    public enum HidingSpotState
    {
        Safe,
        Suspicious,
        BeingSearched,
        Compromised,
        Cooldown
    }

    /// <summary>
    /// An interactable chest or container that allows the player to hide inside it.
    /// </summary>
    public class HidingSpotInteractable : MonoBehaviour, IInteractable
    {
        [Header("Prompts")]
        [Tooltip("Prompt displayed to the player when they can enter the hiding spot.")]
        [SerializeField] private string hidePrompt = "Press E to Hide";

        [Tooltip("Prompt displayed to the player when they are hiding and can exit.")]
        [SerializeField] private string exitPrompt = "Press E to Exit Hiding";

        [Header("Visual Feedback (Optional)")]
        [Tooltip("The SpriteRenderer used to display the chest. If left null, searches children.")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        [Tooltip("Sprite representing the chest when it is empty/open.")]
        [SerializeField] private Sprite openSprite;

        [Tooltip("Sprite representing the chest when the player is hiding inside.")]
        [SerializeField] private Sprite closedSprite;

        private HidingSystem playerHiding;
        private HidingSpotState state = HidingSpotState.Safe;
        private float cooldownTimer;

        public HidingSpotState State => state;

        private void Start()
        {
            // Find player and cache HidingSystem
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHiding = player.GetComponent<HidingSystem>();
                if (playerHiding != null)
                {
                    playerHiding.OnHiddenChanged += HandleHiddenChanged;
                }
            }

            if (targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Set initial sprite
            UpdateVisuals(false);
        }

        private void OnDestroy()
        {
            if (playerHiding != null)
            {
                playerHiding.OnHiddenChanged -= HandleHiddenChanged;
            }
        }

        private void Update()
        {
            if (state == HidingSpotState.BeingSearched && targetSpriteRenderer != null)
            {
                float pulse = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;
                targetSpriteRenderer.color = Color.Lerp(
                    new Color(1f, 0.48f, 0.08f, 1f),
                    new Color(1f, 0.12f, 0.08f, 1f), pulse);
            }

            if (state == HidingSpotState.Cooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f) SetState(HidingSpotState.Safe);
            }
            else if (state == HidingSpotState.Compromised)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f) BeginCooldown();
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            PlayerController controller = interactor.GetComponent<PlayerController>();
            if (controller != null && controller.IsFrozen) return false;

            if (playerHiding == null)
            {
                playerHiding = interactor.GetComponent<HidingSystem>();
                if (playerHiding != null)
                {
                    playerHiding.OnHiddenChanged += HandleHiddenChanged;
                }
            }
            if (playerHiding == null) return false;
            if (playerHiding.IsHidden) return true;
            return state == HidingSpotState.Safe || state == HidingSpotState.Suspicious;
        }

        public void Interact(GameObject interactor)
        {
            PlayerController controller = interactor.GetComponent<PlayerController>();
            if (controller != null && controller.IsFrozen) return;

            if (playerHiding == null)
            {
                playerHiding = interactor.GetComponent<HidingSystem>();
                if (playerHiding != null)
                {
                    playerHiding.OnHiddenChanged += HandleHiddenChanged;
                }
            }

            if (playerHiding != null)
            {
                playerHiding.ToggleHide();
            }
        }

        public string GetPrompt()
        {
            if (playerHiding != null && playerHiding.IsHidden)
            {
                return exitPrompt;
            }
            if (state == HidingSpotState.BeingSearched) return "Killer is searching here!";
            if (state == HidingSpotState.Compromised || state == HidingSpotState.Cooldown)
                return "This hiding spot is compromised";
            return hidePrompt;
        }

        public bool ContainsPlayer(HidingSystem hiding)
        {
            if (hiding == null) return false;
            Collider2D spotCollider = GetComponent<Collider2D>();
            Collider2D playerCollider = hiding.GetComponent<Collider2D>();
            return spotCollider != null && playerCollider != null &&
                spotCollider.bounds.Intersects(playerCollider.bounds);
        }

        public void MarkSuspicious()
        {
            if (state == HidingSpotState.Safe) SetState(HidingSpotState.Suspicious);
        }

        public void BeginSearch()
        {
            SetState(HidingSpotState.BeingSearched);
        }

        public void MarkCompromised()
        {
            SetState(HidingSpotState.Compromised);
            cooldownTimer = 1.25f;
        }

        public void BeginCooldown()
        {
            SetState(HidingSpotState.Cooldown);
            cooldownTimer = 4f;
        }

        private void SetState(HidingSpotState newState)
        {
            state = newState;
            ApplyStateVisual();
        }

        private void ApplyStateVisual()
        {
            if (targetSpriteRenderer == null) return;
            switch (state)
            {
                case HidingSpotState.Safe:
                    targetSpriteRenderer.color = Color.white;
                    break;
                case HidingSpotState.Suspicious:
                    targetSpriteRenderer.color = new Color(1f, 0.72f, 0.2f, 1f);
                    break;
                case HidingSpotState.BeingSearched:
                    targetSpriteRenderer.color = new Color(1f, 0.35f, 0.08f, 1f);
                    break;
                case HidingSpotState.Compromised:
                    targetSpriteRenderer.color = new Color(0.85f, 0.12f, 0.1f, 1f);
                    break;
                case HidingSpotState.Cooldown:
                    targetSpriteRenderer.color = new Color(0.48f, 0.48f, 0.5f, 1f);
                    break;
            }
        }

        private void HandleHiddenChanged(bool hidden)
        {
            // Only update visuals if the player is actually hiding inside THIS specific chest spot.
            // Since there is a trigger collider on this chest, we check if player is near/overlapping.
            // Or simple: check if player is standing in range of this chest when hidden is true.
            if (playerHiding != null)
            {
                // Verify if player is overlapping this chest's collider before updating this specific chest's sprite.
                var myCollider = GetComponent<Collider2D>();
                var playerCollider = playerHiding.GetComponent<Collider2D>();
                if (myCollider != null && playerCollider != null && myCollider.bounds.Intersects(playerCollider.bounds))
                {
                    UpdateVisuals(hidden);
                }
                else if (!hidden)
                {
                    // Always reset to open/empty visual when not hiding
                    UpdateVisuals(false);
                }
            }
        }

        private void UpdateVisuals(bool isPlayerHidingInside)
        {
            if (targetSpriteRenderer == null) return;

            if (isPlayerHidingInside)
            {
                if (closedSprite != null)
                {
                    targetSpriteRenderer.sprite = closedSprite;
                }
                else
                {
                    // Fallback visual feedback: slightly dim the chest if closed sprite is missing
                    targetSpriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                }
            }
            else
            {
                if (openSprite != null)
                {
                    targetSpriteRenderer.sprite = openSprite;
                }
                // Reset color to full white
            }
            ApplyStateVisual();
        }
    }
}
