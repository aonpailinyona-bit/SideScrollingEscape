using System;
using SideScrollingEscape.Core;
using SideScrollingEscape.Data;
using SideScrollingEscape.Environment;
using SideScrollingEscape.Enemy;
using SideScrollingEscape.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.Player
{
    public enum PlayerState
    {
        Normal,
        Hidden,
        Frozen,
        Hurt,
        UsingItem,
        Transitioning,
        Dead
    }

    public enum CurseState
    {
        Safe,
        Exposed,
        Warning,
        Critical,
        Petrified,
        Recovering
    }

    /// <summary>
    /// Drives 2D side-scrolling player movement using the new Input System and
    /// owns the gameplay input actions. Other player components (flashlight,
    /// hiding) subscribe to the events it raises.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Input Actions asset containing the 'Player' action map.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Movement")]
        [Tooltip("Fallback walk speed if no LevelConfig is available.")]
        [SerializeField] private float fallbackMoveSpeed = 4.5f;
        [Tooltip("Fallback sprint speed if no LevelConfig is available.")]
        [SerializeField] private float fallbackSprintSpeed = 5.7f;
        [Tooltip("How quickly horizontal velocity accelerates toward the target.")]
        [SerializeField] private float acceleration = 40f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 5f;
        [SerializeField] private float staminaDrainPerSecond = 1.25f;
        [SerializeField] private float staminaRegenPerSecond = 0.85f;
        [SerializeField] private float staminaRegenDelay = 1f;

        [Header("Jump")]
        [Tooltip("Upward impulse applied when jumping.")]
        [SerializeField] private float jumpForce = 18f;
        [Tooltip("Grace period after leaving the ground during which a jump is still allowed (coyote time).")]
        [SerializeField] private float coyoteTime = 0.15f;
        [Tooltip("Layers considered solid ground for the grounded check.")]
        [SerializeField] private LayerMask groundMask = 0;
        [Tooltip("How far below the collider to probe for ground.")]
        [SerializeField] private float groundCheckDistance = 0.15f;
        [Tooltip("Horizontal inset of the side ground rays from the collider edges.")]
        [SerializeField] private float groundCheckInset = 0.05f;
        [Tooltip("Maximum ledge height the player can walk up without jumping.")]
        [SerializeField] private float maxStepHeight = 1.05f;
        [Tooltip("Horizontal distance used to detect the next stair or bridge segment.")]
        [SerializeField] private float stepProbeDistance = 0.18f;

        [Header("Ladders")]
        [Tooltip("Vertical climb speed while on a ladder.")]
        [SerializeField] private float climbSpeed = 4f;

        [Header("Interaction")]
        [Tooltip("Radius used to detect interactable objects around the player.")]
        [SerializeField] private float interactRadius = 1.2f;
        [Tooltip("Layers considered when searching for interactables.")]
        [SerializeField] private LayerMask interactMask = ~0;

        [Header("Rock distraction")]
        [SerializeField] private int startingRocks = 3;
        [SerializeField] private float rockThrowCooldown = 0.45f;

        [Header("Level 2 Curse")]
        [SerializeField] private float maxCurse = 100f;
        [SerializeField] private float curseDecayPerSecond = 16f;
        [SerializeField] private float curseDecayDelay = 1f;
        [SerializeField] private float curseRecoveryDuration = 2.5f;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Collider2D bodyCollider;
        private Animator animator;

        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");

        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction flashlightAction;
        private InputAction hideAction;
        private InputAction pauseAction;
        private InputAction jumpAction;
        private InputActionMap playerMap;

        private float moveSpeed;
        private float sprintSpeed;
        private bool inputBound;
        private float currentStamina;
        private float lastStaminaUseTime = -999f;
        private bool staminaExhausted;
        private int currentRocks;
        private float nextRockThrowTime;
        private float currentCurse;
        private float lastCurseExposureTime = -999f;
        private bool curseExposed;
        private float curseRecoveryTimer;

        // Jump / coyote time bookkeeping.
        private float lastGroundedTime = -999f;
        private float lastJumpTime = -999f;
        private const float JumpRefreshLock = 0.1f;

        // Ladder / climbing bookkeeping.
        private float defaultGravityScale = 3f;
        private int playerLayer;
        private int groundLayer = -1;
        private static PhysicsMaterial2D frictionlessMaterial;
        private int ladderOverlapCount;
        private Ladder currentLadder;
        private bool isClimbing;

        /// <summary>Raised when the flashlight toggle action is pressed.</summary>
        public event Action FlashlightToggled;

        /// <summary>Raised when the hide action is pressed.</summary>
        public event Action HideToggled;

        /// <summary>Raised when the interact action is pressed.</summary>
        public event Action InteractPressed;

        /// <summary>Raised when stamina changes. Values are current and maximum.</summary>
        public event Action<float, float> OnStaminaChanged;
        public event Action<int, int> OnRockCountChanged;
        public event Action<float, float, bool> OnCurseChanged;
        public event Action<CurseState> OnCurseStateChanged;
        public event Action<bool> OnCurseStatueResult;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public int CurrentRocks => currentRocks;
        public int MaxRocks => startingRocks;
        public float CurrentCurse => currentCurse;
        public float MaxCurse => maxCurse;
        public CurseState CurrentCurseState { get; private set; } = CurseState.Safe;

        /// <summary>Current raw movement input.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>True while the sprint action is held.</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>True while the player is moving horizontally.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>+1 when facing right, -1 when facing left.</summary>
        public int FacingDirection { get; private set; } = 1;

        /// <summary>True while the player is standing on ground (set by the ground check).</summary>
        public bool IsGrounded { get; private set; }

        private bool externalMovementLock;

        /// <summary>Current exclusive gameplay state.</summary>
        public PlayerState State { get; private set; } = PlayerState.Normal;
        public event Action<PlayerState> OnPlayerStateChanged;

        /// <summary>Compatibility lock for scripted sequences; FSM states are also respected.</summary>
        public bool MovementLocked
        {
            get => externalMovementLock || State != PlayerState.Normal;
            set => externalMovementLock = value;
        }

        public bool CanUseGameplayInput => State == PlayerState.Normal;
        public bool IsInteractHeld => interactAction != null && interactAction.IsPressed();

        /// <summary>The interactable currently in range, or null.</summary>
        public IInteractable CurrentInteractable { get; private set; }

        /// <summary>Raised when the player is frozen or unfrozen.</summary>
        public event Action<bool> OnFrozenStateChanged;
        
        /// <summary>True while the player is frozen and needs to mash to escape.</summary>
        public bool IsFrozen { get; private set; }
        private int freezeBreakCount = 0;
        private int freezeRequiredMashes = 5;
        
        [Header("Freeze UI")]
        [SerializeField] private GameObject freezeCanvas;
        [SerializeField] private UnityEngine.UI.Image freezeProgressBar;

        /// <summary>Freezes the player, requiring button mashing to escape.</summary>
        public void Freeze(int requiredMashes = 5)
        {
            if (IsFrozen || State == PlayerState.Dead) return;
            IsFrozen = true;
            freezeBreakCount = 0;
            freezeRequiredMashes = requiredMashes;
            SetPlayerState(PlayerState.Frozen);
            if (body != null)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray; // Gray
            }
            
            if (freezeCanvas != null) freezeCanvas.SetActive(true);
            if (freezeProgressBar != null) freezeProgressBar.fillAmount = 0f;
            
            OnFrozenStateChanged?.Invoke(true);
        }

        public void Unfreeze()
        {
            if (!IsFrozen) return;
            IsFrozen = false;
            if (State == PlayerState.Frozen) SetPlayerState(PlayerState.Normal);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            if (freezeCanvas != null) freezeCanvas.SetActive(false);
            
            OnFrozenStateChanged?.Invoke(false);
            if (CurrentCurseState == CurseState.Petrified)
                BeginCurseRecovery(curseRecoveryDuration);
        }

        /// <summary>Stuns the player, disabling movement for the specified duration.</summary>
        public void Stun(float duration)
        {
            if (State == PlayerState.Dead || State == PlayerState.Frozen) return;
            StartCoroutine(StunRoutine(duration));
        }

        private System.Collections.IEnumerator StunRoutine(float duration)
        {
            SetPlayerState(PlayerState.Hurt);
            if (body != null)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }

            float blinkInterval = 0.15f;
            float elapsed = 0f;
            bool isVisible = true;
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

            while (elapsed < duration)
            {
                if (sr != null)
                {
                    isVisible = !isVisible;
                    sr.enabled = isVisible;
                }
                yield return new WaitForSeconds(Mathf.Min(blinkInterval, duration - elapsed));
                elapsed += blinkInterval;
            }

            if (sr != null) sr.enabled = true;
            if (State == PlayerState.Hurt) SetPlayerState(PlayerState.Normal);
        }

        public void SetHiddenState(bool hidden)
        {
            if (State == PlayerState.Dead || State == PlayerState.Frozen) return;
            if (hidden) SetPlayerState(PlayerState.Hidden);
            else if (State == PlayerState.Hidden) SetPlayerState(PlayerState.Normal);
        }

        public void SetInventoryState(bool open)
        {
            if (open && State == PlayerState.Normal) SetPlayerState(PlayerState.UsingItem);
            else if (!open && State == PlayerState.UsingItem) SetPlayerState(PlayerState.Normal);
        }

        public bool BeginScriptedAction()
        {
            if (State != PlayerState.Normal) return false;
            SetPlayerState(PlayerState.UsingItem);
            if (body != null) body.linearVelocity = Vector2.zero;
            return true;
        }

        public void EndScriptedAction()
        {
            if (State == PlayerState.UsingItem) SetPlayerState(PlayerState.Normal);
        }

        public void EnterHurtState()
        {
            if (State == PlayerState.Normal || State == PlayerState.UsingItem)
                SetPlayerState(PlayerState.Hurt);
        }

        public void ExitHurtState()
        {
            if (State == PlayerState.Hurt) SetPlayerState(PlayerState.Normal);
        }

        public void EnterDeadState()
        {
            IsFrozen = false;
            externalMovementLock = false;
            SetPlayerState(PlayerState.Dead);
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        public bool BeginTransition()
        {
            if (State != PlayerState.Normal) return false;
            SetPlayerState(PlayerState.Transitioning);
            if (body != null) body.linearVelocity = Vector2.zero;
            return true;
        }

        public void EndTransition()
        {
            if (State == PlayerState.Transitioning) SetPlayerState(PlayerState.Normal);
        }

        private void SetPlayerState(PlayerState newState)
        {
            if (State == newState) return;
            State = newState;
            OnPlayerStateChanged?.Invoke(newState);
        }

        private void Awake()
        {
            if (freezeCanvas != null) freezeCanvas.SetActive(false);
            
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            bodyCollider = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();

            // Pixel-art slopes can contain tiny vertical edges that catch a
            // moving capsule.  A zero-friction player material lets the body
            // slide across those seams without affecting ground collision.
            if (bodyCollider != null)
            {
                if (frictionlessMaterial == null)
                {
                    frictionlessMaterial = new PhysicsMaterial2D("Player_Frictionless")
                    {
                        friction = 0f,
                        bounciness = 0f
                    };
                }

                bodyCollider.sharedMaterial = frictionlessMaterial;
            }

            defaultGravityScale = body.gravityScale;
            playerLayer = gameObject.layer;
            groundLayer = GetLayerFromMask(groundMask);

            LevelConfig config = GameManager.Instance != null ? GameManager.Instance.CurrentLevelConfig : null;
            moveSpeed = config != null ? config.PlayerMoveSpeed : fallbackMoveSpeed;
            sprintSpeed = config != null ? config.PlayerSprintSpeed : fallbackSprintSpeed;
            currentStamina = maxStamina;
            currentRocks = Mathf.Max(0, startingRocks);

            BindInput();
        }

        private void BindInput()
        {
            if (inputBound || inputActions == null)
            {
                return;
            }

            playerMap = inputActions.FindActionMap("Player", throwIfNotFound: false);
            if (playerMap == null)
            {
                Debug.LogError("[PlayerController] 'Player' action map not found.");
                return;
            }

            moveAction = playerMap.FindAction("Move");
            sprintAction = playerMap.FindAction("Sprint");
            interactAction = playerMap.FindAction("Interact");
            flashlightAction = playerMap.FindAction("ToggleFlashlight");
            hideAction = playerMap.FindAction("Hide");
            pauseAction = playerMap.FindAction("Pause");
            jumpAction = playerMap.FindAction("Jump");

            if (interactAction != null) interactAction.performed += OnInteract;
            if (flashlightAction != null) flashlightAction.performed += OnFlashlight;
            if (hideAction != null) hideAction.performed += OnHide;
            if (pauseAction != null) pauseAction.performed += OnPause;
            if (jumpAction != null) jumpAction.performed += OnJump;

            inputBound = true;
        }

        private void OnEnable()
        {
            playerMap?.Enable();
        }

        private void OnDisable()
        {
            playerMap?.Disable();

            // Make sure we never leave ground collisions globally disabled.
            if (isClimbing)
            {
                ExitClimb();
            }
        }

        private void OnDestroy()
        {
            if (interactAction != null) interactAction.performed -= OnInteract;
            if (flashlightAction != null) flashlightAction.performed -= OnFlashlight;
            if (hideAction != null) hideAction.performed -= OnHide;
            if (pauseAction != null) pauseAction.performed -= OnPause;
            if (jumpAction != null) jumpAction.performed -= OnJump;
        }

        private void Update()
        {
            if (IsFrozen)
            {
                bool mashed = false;
                if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) mashed = true;
                if (interactAction != null && interactAction.triggered) mashed = true;

                if (mashed)
                {
                    freezeBreakCount++;
                    if (freezeProgressBar != null)
                    {
                        freezeProgressBar.fillAmount = (float)freezeBreakCount / freezeRequiredMashes;
                    }

                    if (freezeBreakCount >= freezeRequiredMashes)
                    {
                        Unfreeze();
                    }
                }
            }

            bool playing = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;

            if (playing && CanUseGameplayInput && Keyboard.current != null &&
                Keyboard.current.qKey.wasPressedThisFrame)
            {
                TryThrowRock();
            }

            MoveInput = (playing && moveAction != null) ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            bool sprintHeld = playing && sprintAction != null && sprintAction.IsPressed();
            bool movingHorizontally = Mathf.Abs(MoveInput.x) > 0.1f && !MovementLocked && !isClimbing;

            if (staminaExhausted && currentStamina >= maxStamina * 0.25f)
                staminaExhausted = false;

            IsSprinting = sprintHeld && movingHorizontally && !staminaExhausted && currentStamina > 0f;

            float previousStamina = currentStamina;
            if (IsSprinting)
            {
                currentStamina = Mathf.Max(0f, currentStamina - staminaDrainPerSecond * Time.deltaTime);
                lastStaminaUseTime = Time.time;
                if (currentStamina <= 0f)
                {
                    currentStamina = 0f;
                    staminaExhausted = true;
                    IsSprinting = false;
                }
            }
            else if (playing && Time.time - lastStaminaUseTime >= staminaRegenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSecond * Time.deltaTime);
            }

            if (!Mathf.Approximately(previousStamina, currentStamina))
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            UpdateCurseFSM(playing);

            UpdateInteractableInRange();
            UpdateFacing();

            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, IsMoving);
            }
        }

        /// <summary>Builds Level 2's petrification curse while a killer has line of sight.</summary>
        public void ApplyCursePressure(float amount, KillerAI source)
        {
            if (amount <= 0f || IsFrozen || CurrentCurseState == CurseState.Recovering ||
                State == PlayerState.Dead || SceneManager.GetActiveScene().name != "Level2") return;

            lastCurseExposureTime = Time.time;
            curseExposed = true;
            currentCurse = Mathf.Min(maxCurse, currentCurse + amount);

            if (currentCurse >= maxCurse)
            {
                if (InventoryManager.Instance != null &&
                    InventoryManager.Instance.TryConsumeItemById("curse_mirror"))
                {
                    currentCurse = 0f;
                    curseExposed = false;
                    source?.ReflectCurse();
                    BeginCurseRecovery(curseRecoveryDuration);
                }
                else
                {
                    currentCurse = 0f;
                    curseExposed = false;
                    SetCurseState(CurseState.Petrified);
                    Freeze(7);
                }
            }

            OnCurseChanged?.Invoke(currentCurse, maxCurse, curseExposed);
            if (CurrentCurseState != CurseState.Petrified && CurrentCurseState != CurseState.Recovering)
                RefreshCurseState(true);
        }

        /// <summary>Removes all accumulated petrification at a genuine Level 2 statue.</summary>
        public void CleanseCurse()
        {
            if (SceneManager.GetActiveScene().name != "Level2") return;
            currentCurse = 0f;
            curseExposed = false;
            lastCurseExposureTime = -999f;
            OnCurseChanged?.Invoke(currentCurse, maxCurse, false);
            BeginCurseRecovery(1.25f);
            OnCurseStatueResult?.Invoke(true);
        }

        /// <summary>Adds an immediate burst of petrification from a revealed fake statue.</summary>
        public void ApplyStatueCurse(float amount)
        {
            ApplyCursePressure(Mathf.Max(0f, amount), null);
            OnCurseStatueResult?.Invoke(false);
        }

        private void UpdateCurseFSM(bool playing)
        {
            if (SceneManager.GetActiveScene().name != "Level2") return;

            if (CurrentCurseState == CurseState.Recovering)
            {
                if (playing) curseRecoveryTimer -= Time.deltaTime;
                if (curseRecoveryTimer <= 0f) SetCurseState(CurseState.Safe);
                return;
            }
            if (CurrentCurseState == CurseState.Petrified) return;

            bool nowExposed = Time.time - lastCurseExposureTime <= 0.12f;
            if (!nowExposed && curseExposed)
            {
                curseExposed = false;
                OnCurseChanged?.Invoke(currentCurse, maxCurse, false);
            }

            if (playing && !IsFrozen && !nowExposed && currentCurse > 0f &&
                Time.time - lastCurseExposureTime >= curseDecayDelay)
            {
                currentCurse = Mathf.Max(0f, currentCurse - curseDecayPerSecond * Time.deltaTime);
                OnCurseChanged?.Invoke(currentCurse, maxCurse, false);
            }

            RefreshCurseState(nowExposed);
        }

        private void RefreshCurseState(bool exposed)
        {
            float normalized = maxCurse > 0f ? currentCurse / maxCurse : 0f;
            CurseState next = normalized >= 0.75f ? CurseState.Critical :
                normalized >= 0.4f ? CurseState.Warning :
                exposed && normalized > 0.001f ? CurseState.Exposed : CurseState.Safe;
            SetCurseState(next);
        }

        private void BeginCurseRecovery(float duration)
        {
            currentCurse = 0f;
            curseExposed = false;
            lastCurseExposureTime = -999f;
            curseRecoveryTimer = Mathf.Max(0.1f, duration);
            SetCurseState(CurseState.Recovering);
            OnCurseChanged?.Invoke(currentCurse, maxCurse, false);
        }

        private void SetCurseState(CurseState newState)
        {
            if (CurrentCurseState == newState) return;
            CurrentCurseState = newState;
            OnCurseStateChanged?.Invoke(newState);
        }

        private void TryThrowRock()
        {
            if (currentRocks <= 0 || Time.unscaledTime < nextRockThrowTime) return;

            nextRockThrowTime = Time.unscaledTime + rockThrowCooldown;
            currentRocks--;

            GameObject rockObject = new GameObject("ThrownRock");
            rockObject.transform.position = transform.position +
                new Vector3(FacingDirection * 0.65f, 0.45f, 0f);
            ThrownRock rock = rockObject.AddComponent<ThrownRock>();
            rock.Launch(bodyCollider, FacingDirection);

            OnRockCountChanged?.Invoke(currentRocks, startingRocks);
        }

        private void FixedUpdate()
        {
            bool playing = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;

            // Begin climbing when overlapping a ladder and pushing up/down.
            if (playing && !MovementLocked && !isClimbing
                && ladderOverlapCount > 0 && currentLadder != null
                && Mathf.Abs(MoveInput.y) > 0.5f)
            {
                EnterClimb();
            }

            if (isClimbing)
            {
                ClimbFixedUpdate(playing);
                return;
            }

            float targetX = 0f;

            if (playing && !MovementLocked)
            {
                float speed = IsSprinting ? sprintSpeed : moveSpeed;
                targetX = Mathf.Clamp(MoveInput.x, -1f, 1f) * speed;
            }

            float newX = Mathf.MoveTowards(body.linearVelocity.x, targetX, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(newX, body.linearVelocity.y);
            IsMoving = Mathf.Abs(newX) > 0.05f;

            IsGrounded = CheckGrounded();

            bool followingBridge = playing && !MovementLocked && Mathf.Abs(targetX) > 0.05f &&
                TryFollowLevel3BridgeSlope(targetX);

            if (playing && !MovementLocked && IsGrounded && Mathf.Abs(targetX) > 0.05f && !followingBridge)
            {
                TryStepUp(Mathf.Sign(targetX));
            }

            // Refresh the coyote-time window while grounded, but not immediately
            // after a jump (so leaving the ground via jump does not grant a second one).
            if (IsGrounded && Time.time - lastJumpTime > JumpRefreshLock)
            {
                lastGroundedTime = Time.time;
            }
        }

        /// <summary>
        /// Drives vertical movement while attached to a ladder. Gravity is disabled
        /// and collisions with ground are ignored so the player can pass through
        /// thin platforms, snapping horizontally to the ladder's center.
        /// </summary>
        private void ClimbFixedUpdate(bool playing)
        {
            if (!playing || MovementLocked || currentLadder == null || ladderOverlapCount <= 0)
            {
                ExitClimb();
                return;
            }

            float vy = Mathf.Clamp(MoveInput.y, -1f, 1f) * climbSpeed;

            // Stop and drop off when climbing down past the bottom of the ladder.
            if (vy < 0f && body.position.y <= currentLadder.BottomY + 0.02f)
            {
                ExitClimb();
                return;
            }

            body.linearVelocity = new Vector2(0f, vy);
            body.position = new Vector2(currentLadder.CenterX, body.position.y);

            IsMoving = false;
            IsGrounded = false;
        }

        /// <summary>
        /// Casts short rays from the bottom of the collider to determine whether
        /// the player is standing on ground. Used to prevent double jumping.
        /// </summary>
        /// <returns>True if ground is detected directly below the player.</returns>
        private bool CheckGrounded()
        {
            if (bodyCollider == null)
            {
                return false;
            }

            Bounds b = bodyCollider.bounds;
            float y = b.min.y + 0.02f;
            float left = b.min.x + groundCheckInset;
            float right = b.max.x - groundCheckInset;
            float mid = b.center.x;

            // A small downward probe from three points along the collider's base
            // so the check is robust on platform edges.
            return CastDown(new Vector2(mid, y))
                || CastDown(new Vector2(left, y))
                || CastDown(new Vector2(right, y));
        }

        private bool CastDown(Vector2 origin)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundMask);
            return hit.collider != null;
        }

        private void TryStepUp(float direction)
        {
            if (bodyCollider == null) return;

            Bounds bounds = bodyCollider.bounds;
            float sideX = bounds.center.x + direction * (bounds.extents.x + 0.01f);
            Vector2 lowOrigin = new Vector2(sideX - direction * 0.04f, bounds.min.y + 0.08f);
            RaycastHit2D obstacle = Physics2D.Raycast(
                lowOrigin,
                Vector2.right * direction,
                stepProbeDistance,
                groundMask);

            if (obstacle.collider == null) return;

            float probeX = sideX + direction * stepProbeDistance;
            Vector2 topOrigin = new Vector2(probeX, bounds.min.y + maxStepHeight + 0.08f);
            RaycastHit2D top = Physics2D.Raycast(
                topOrigin,
                Vector2.down,
                maxStepHeight + 0.08f,
                groundMask);

            if (top.collider == null) return;

            float rise = top.point.y - bounds.min.y;
            if (rise <= 0.02f || rise > maxStepHeight) return;

            body.position += Vector2.up * (rise + 0.02f);
        }

        private bool TryFollowLevel3BridgeSlope(float targetX)
        {
            if (bodyCollider == null || body.linearVelocity.y > 1.2f) return false;

            Bounds bounds = bodyCollider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.22f);
            RaycastHit2D bridgeHit = default;
            bool foundBridge = false;

            // The bridge overlaps the normal ground at both entrances. RaycastAll
            // ensures the tilemap cannot hide the smooth bridge collider.
            foreach (RaycastHit2D candidate in Physics2D.RaycastAll(origin, Vector2.down, 0.85f, groundMask))
            {
                if (candidate.collider != null &&
                    candidate.collider.gameObject.name == "Level3_SmoothBridgeCollider")
                {
                    bridgeHit = candidate;
                    foundBridge = true;
                    break;
                }
            }

            if (!foundBridge) return false;

            // The middle of the bridge is flat and needs no slope assistance.
            if (bridgeHit.normal.y < 0.55f || Mathf.Abs(bridgeHit.normal.x) < 0.02f)
                return false;

            // Keep the feet touching the invisible ramp, without snapping a
            // jumping player down onto it.
            float verticalCorrection = bridgeHit.point.y - bounds.min.y + 0.025f;
            body.position += Vector2.up * Mathf.Clamp(verticalCorrection, -0.12f, 0.12f);

            Vector2 tangent = new Vector2(bridgeHit.normal.y, -bridgeHit.normal.x);
            if (Mathf.Sign(tangent.x) != Mathf.Sign(targetX))
                tangent = -tangent;

            float horizontalSpeed = Mathf.Abs(targetX);
            body.linearVelocity = tangent * (horizontalSpeed / Mathf.Max(Mathf.Abs(tangent.x), 0.1f));
            return true;
        }

        private void UpdateFacing()
        {
            if (Mathf.Abs(MoveInput.x) > 0.1f)
            {
                FacingDirection = MoveInput.x > 0f ? 1 : -1;
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = FacingDirection < 0;
                }
            }
        }

        private void UpdateInteractableInRange()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactMask);
            IInteractable best = null;
            float bestDist = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent(out IInteractable interactable) && interactable.CanInteract(gameObject))
                {
                    float d = (hit.transform.position - transform.position).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = interactable;
                    }
                }
            }

            CurrentInteractable = best;
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            // Hidden players may still interact to leave the current hiding spot.
            if (State != PlayerState.Normal && State != PlayerState.Hidden) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            InteractPressed?.Invoke();
            CurrentInteractable?.Interact(gameObject);
        }

        private void OnFlashlight(InputAction.CallbackContext ctx)
        {
            if (CanUseGameplayInput &&
                (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing))
            {
                FlashlightToggled?.Invoke();
            }
        }

        private void OnHide(InputAction.CallbackContext ctx)
        {
            // C belongs exclusively to InventoryUI. Older imported input assets may still
            // report Hide on C for one editor refresh, so guard it here as well.
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) return;

            if ((State == PlayerState.Normal || State == PlayerState.Hidden) &&
                (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing))
            {
                HideToggled?.Invoke();
            }
        }

        private void OnPause(InputAction.CallbackContext ctx)
        {
            GameManager.Instance?.TogglePause();
        }

        /// <summary>
        /// Applies an upward jump impulse, but only when grounded, unlocked and
        /// during active gameplay. The grounded check prevents double jumping.
        /// </summary>
        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (MovementLocked)
            {
                return;
            }

            // Jumping off a ladder detaches the player and applies a normal jump.
            if (isClimbing)
            {
                ExitClimb();
                lastJumpTime = Time.time;
                lastGroundedTime = -999f;
                body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
                body.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                return;
            }

            // Allow the jump when grounded or within the coyote-time grace window,
            // but never twice in quick succession.
            bool withinCoyote = Time.time - lastGroundedTime <= coyoteTime;
            if ((!IsGrounded && !withinCoyote) || Time.time - lastJumpTime <= JumpRefreshLock)
            {
                return;
            }

            lastJumpTime = Time.time;
            lastGroundedTime = -999f;

            // Reset any downward velocity first for a consistent jump height.
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
            body.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Ladder ladder))
            {
                ladderOverlapCount++;
                currentLadder = ladder;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out Ladder ladder))
            {
                ladderOverlapCount = Mathf.Max(0, ladderOverlapCount - 1);
                if (ladderOverlapCount == 0)
                {
                    currentLadder = null;
                    if (isClimbing)
                    {
                        ExitClimb();
                    }
                }
            }
        }

        /// <summary>Attaches the player to the current ladder, zeroing velocity and gravity.</summary>
        private void EnterClimb()
        {
            if (isClimbing || currentLadder == null)
            {
                return;
            }

            isClimbing = true;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;

            // Pass through thin platforms while climbing so the player can reach the top.
            if (groundLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, true);
            }
        }

        /// <summary>Detaches the player from the ladder, restoring gravity and collisions.</summary>
        private void ExitClimb()
        {
            if (!isClimbing)
            {
                return;
            }

            isClimbing = false;
            body.gravityScale = defaultGravityScale;

            if (groundLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, false);
            }
        }

        /// <summary>Returns the first layer index set in <paramref name="mask"/>, or -1.</summary>
        private static int GetLayerFromMask(LayerMask mask)
        {
            int value = mask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);

            Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (col != null)
            {
                Bounds b = col.bounds;
                float y = b.min.y + 0.02f;
                float left = b.min.x + groundCheckInset;
                float right = b.max.x - groundCheckInset;
                float mid = b.center.x;
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawLine(new Vector3(mid, y), new Vector3(mid, y - groundCheckDistance));
                Gizmos.DrawLine(new Vector3(left, y), new Vector3(left, y - groundCheckDistance));
                Gizmos.DrawLine(new Vector3(right, y), new Vector3(right, y - groundCheckDistance));
            }
        }
    }
}
