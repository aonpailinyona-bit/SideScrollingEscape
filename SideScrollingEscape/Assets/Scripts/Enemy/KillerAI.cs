using System;
using System.Collections.Generic;
using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using SideScrollingEscape.Data;
using SideScrollingEscape.Player;
using SideScrollingEscape.Items;
using UnityEngine;

namespace SideScrollingEscape.Enemy
{
    /// <summary>
    /// Behavioural states for the killer.
    /// </summary>
    public enum KillerState
    {
        /// <summary>Walking a patrol route.</summary>
        Patrol,
        /// <summary>Pausing to face a newly heard disturbance.</summary>
        Alert,
        /// <summary>Stops briefly to judge a sound's direction and importance.</summary>
        Listening,
        /// <summary>Travels to the selected sound source.</summary>
        InvestigateSound,
        /// <summary>Checks the sound source before committing to a search.</summary>
        ConfirmSound,
        /// <summary>Brief hesitation after discovering a repeated rock distraction.</summary>
        Confused,
        /// <summary>Moving toward the player's last known position.</summary>
        Investigate,
        /// <summary>Sweeping around the last known position.</summary>
        Search,
        /// <summary>Actively pursuing the visible/audible player.</summary>
        Chase,
        /// <summary>In melee range and attacking.</summary>
        Attack,
        /// <summary>Brief vulnerable pause after committing to an attack.</summary>
        AttackRecover,
        /// <summary>Returning naturally to the closest patrol waypoint.</summary>
        ReturnToPatrol,
        /// <summary>Screaming to stun player.</summary>
        Scream
    }

    public enum NoiseType
    {
        Footstep,
        Sprint,
        Rock,
        Glass,
        Environmental
    }

    /// <summary>
    /// Lightweight 2D side-scroller killer AI. Patrols between waypoints, detects
    /// the player via line-of-sight (boosted by the flashlight) and hearing
    /// (walking/sprinting), chases, and attacks. Respects the hiding system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class KillerAI : MonoBehaviour
    {
        [Header("Patrol")]
        [Tooltip("Patrol waypoints (only X is used). Killer cycles through them.")]
        [SerializeField] private Transform[] patrolPoints;
        [Tooltip("Distance at which a patrol point counts as reached.")]
        [SerializeField] private float waypointTolerance = 0.4f;
        [Tooltip("Seconds to wait at each patrol point.")]
        [SerializeField] private float waypointPause = 1f;

        [Header("Detection")]
        [Tooltip("Field-of-view half-angle (degrees) for sight, ignored when the player's flashlight is on.")]
        [SerializeField] private float sightHalfAngle = 55f;
        [Tooltip("Seconds of continuous detection required before a calm killer fully commits to chase.")]
        [SerializeField] private float suspicionBuildTime = 0.45f;
        [Tooltip("Seconds for suspicion to fade after losing the player.")]
        [SerializeField] private float suspicionFadeTime = 1.4f;

        [Header("Sound decision FSM")]
        [SerializeField] private float listenDuration = 0.38f;
        [SerializeField] private float confirmSoundDuration = 0.75f;
        [SerializeField] private float confusedDuration = 1.1f;
        [SerializeField] private float repeatedRockRadius = 2.25f;
        [SerializeField] private float soundMemorySeconds = 5f;
        [Tooltip("Multiplier applied to sight range when the player's flashlight is on.")]
        [SerializeField] private float flashlightSightMultiplier = 1.6f;
        [Tooltip("Layers that block line of sight.")]
        [SerializeField] private LayerMask obstacleMask;
        [Tooltip("Vertical offset of the killer's 'eyes' used for raycasts.")]
        [SerializeField] private float eyeHeight = 0.6f;

        [Header("Fallback tuning (used if no LevelConfig)")]
        [SerializeField] private float fallbackPatrolSpeed = 2.5f;
        [SerializeField] private float fallbackChaseSpeed = 5.5f;
        [SerializeField] private float fallbackSightRange = 8f;
        [SerializeField] private float fallbackHearingRange = 6f;
        [SerializeField] private float fallbackAttackRange = 1.2f;
        [SerializeField] private float fallbackAttackDamage = 35f;
        [SerializeField] private float fallbackAttackCooldown = 1.5f;
        [SerializeField] private float fallbackSearchDuration = 5f;

        [Header("Audio")]
        [SerializeField] private AudioClip detectClip;
        [SerializeField] private AudioClip attackClip;

        [Header("Collision")]
        [Tooltip("Prevents the player and killer body colliders from blocking or carrying each other.")]
        [SerializeField] private bool ignorePlayerBodyCollision;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private PhysicsMaterial2D frictionlessMaterial;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        private Transform player;
        private Rigidbody2D playerBody;
        private PlayerController playerController;
        private HidingSystem playerHiding;
        private PlayerHealth playerHealth;
        private FlashlightController playerFlashlight;

        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int Attack1Hash = Animator.StringToHash("attack1");
        private static readonly int ScreamHash = Animator.StringToHash("scream");

        [Header("Scream")]
        [SerializeField] private bool canScream = false;
        [SerializeField] private bool freezeInsteadOfStun = false;
        [SerializeField] private float screamDuration = 0.8f;
        private float screamTimer;
        [SerializeField] private float screamCooldown = 5.5f;
        private float screamCooldownTimer;
        private bool curseReflected;

        // tuning
        [SerializeField] private bool isBlind = false;
        private float patrolSpeed;
        private float chaseSpeed;
        private float directorChaseMultiplier = 1f;
        private float sightRange;
        private float hearingRange;
        private float attackRange;
        private float attackDamage;
        private float attackCooldown;
        private float searchDuration;

        // state
        private KillerState state = KillerState.Patrol;
        private int patrolIndex;
        private float pauseTimer;
        private float searchTimer;
        private float attackTimer;
        private int facing = 1;
        private Vector2 lastKnownPosition;
        private float chaseMemoryTimer;
        private const float BlindChaseMemory = 2.5f;
        private float wallAvoidTimer;
        private float wallRepathTimer;
        private float wallAvoidDirection;
        private float requestedMoveDirection;
        private float stuckTimer;
        private float lastProgressX;
        private HidingSpotInteractable suspectedHidingSpot;
        private float hidingSearchTimer = -1f;
        private float alertTimer;
        private float attackRecoverTimer;
        private float lostDetectionTimer;
        private float chaseCommitTimer;
        private float nextDetectSoundTime;
        private Vector2 searchCenter;
        private float searchSweepDirection = 1f;
        private float suspicion;
        private readonly List<SoundStimulus> soundQueue = new List<SoundStimulus>(6);
        private SoundStimulus activeSound;
        private bool hasActiveSound;
        private Vector2 lastRockPosition;
        private float lastRockTime = -999f;
        private int repeatedRockCount;
        private float soundStateTimer;

        private struct SoundStimulus
        {
            public Vector2 Position;
            public float Loudness;
            public float Time;
            public NoiseType Type;
            public bool RepeatedRock;
        }

        /// <summary>Raised whenever the killer's state changes.</summary>
        public event Action<KillerState> OnStateChanged;

        /// <summary>Current AI state.</summary>
        public KillerState State => state;
        public float Suspicion01 => suspicion;
        public void SetDirectorChaseMultiplier(float multiplier) =>
            directorChaseMultiplier = Mathf.Clamp(multiplier, 0.9f, 1.15f);

        /// <summary>True while the killer is actively chasing or attacking.</summary>
        public bool IsHunting => state == KillerState.Chase || state == KillerState.Attack ||
            state == KillerState.AttackRecover;

        /// <summary>Alerts the killer to a noise made by an object in the level.</summary>
        public void HearNoise(Vector2 sourcePosition, float loudness = 1f,
            NoiseType noiseType = NoiseType.Environmental)
        {
            float audibleDistance = hearingRange * Mathf.Max(0f, loudness);
            if (Vector2.Distance(transform.position, sourcePosition) > audibleDistance)
                return;

            bool repeatedRock = false;
            if (noiseType == NoiseType.Rock)
            {
                if (Time.time - lastRockTime <= 12f && Vector2.Distance(lastRockPosition, sourcePosition) <= repeatedRockRadius)
                    repeatedRockCount++;
                else
                    repeatedRockCount = 1;
                lastRockPosition = sourcePosition;
                lastRockTime = Time.time;
                repeatedRock = repeatedRockCount >= 3;
            }

            SoundStimulus stimulus = new SoundStimulus
            {
                Position = sourcePosition,
                Loudness = Mathf.Max(0.1f, loudness),
                Time = Time.time,
                Type = noiseType,
                RepeatedRock = repeatedRock
            };
            if (soundQueue.Count >= 6) soundQueue.RemoveAt(0);
            soundQueue.Add(stimulus);
            suspicion = Mathf.Clamp01(suspicion + Mathf.Clamp01(loudness * 0.35f));

            if (state != KillerState.Attack && state != KillerState.AttackRecover && state != KillerState.Scream)
            {
                bool resolvingSound = state == KillerState.Listening || state == KillerState.InvestigateSound ||
                    state == KillerState.ConfirmSound;
                if (!resolvingSound || !hasActiveSound || ScoreSound(stimulus) > ScoreSound(activeSound) + 0.35f)
                    SelectBestSound();
            }
        }

        private float ScoreSound(SoundStimulus sound)
        {
            float age = Time.time - sound.Time;
            float distance = Vector2.Distance(transform.position, sound.Position);
            float typeWeight = sound.Type == NoiseType.Glass ? 1.8f :
                sound.Type == NoiseType.Sprint ? 1.45f :
                sound.Type == NoiseType.Footstep ? 1.05f :
                sound.Type == NoiseType.Rock ? 1.2f : 1.3f;
            if (sound.RepeatedRock) typeWeight *= 0.28f;
            return sound.Loudness * typeWeight - distance / Mathf.Max(1f, hearingRange) - age * 0.18f;
        }

        private void SelectBestSound()
        {
            soundQueue.RemoveAll(sound => Time.time - sound.Time > soundMemorySeconds);
            if (soundQueue.Count == 0) return;

            int bestIndex = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < soundQueue.Count; i++)
            {
                float score = ScoreSound(soundQueue[i]);
                if (score > bestScore) { bestScore = score; bestIndex = i; }
            }

            activeSound = soundQueue[bestIndex];
            soundQueue.RemoveAt(bestIndex);
            hasActiveSound = true;
            lastKnownPosition = activeSound.Position;
            searchTimer = Mathf.Max(searchDuration, 4f);
            pauseTimer = 0f;
            soundStateTimer = listenDuration;
            SetState(KillerState.Listening);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            bodyCollider = GetComponent<Collider2D>();

            // Match the player's zero-friction capsule so the killer does not
            // catch on the small collider segments while walking downhill.
            if (bodyCollider != null)
            {
                frictionlessMaterial = new PhysicsMaterial2D("Killer_Frictionless")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                bodyCollider.sharedMaterial = frictionlessMaterial;
            }

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponent<Animator>();

            LevelConfig config = GameManager.Instance != null ? GameManager.Instance.CurrentLevelConfig : null;
            patrolSpeed = config != null ? config.KillerPatrolSpeed : fallbackPatrolSpeed;
            chaseSpeed = config != null ? config.KillerChaseSpeed : fallbackChaseSpeed;
            sightRange = config != null ? config.KillerSightRange : fallbackSightRange;
            hearingRange = config != null ? config.KillerHearingRange : fallbackHearingRange;
            attackRange = config != null ? config.KillerAttackRange : fallbackAttackRange;
            attackDamage = config != null ? config.KillerAttackDamage : fallbackAttackDamage;
            attackCooldown = config != null ? config.KillerAttackCooldown : fallbackAttackCooldown;
            searchDuration = config != null ? config.KillerSearchDuration : fallbackSearchDuration;
        }

        private void Start()
        {
            lastProgressX = transform.position.x;
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                CachePlayer(playerObj);
            }
        }

        private void CachePlayer(GameObject playerObj)
        {
            player = playerObj.transform;
            playerBody = playerObj.GetComponent<Rigidbody2D>();
            playerController = playerObj.GetComponent<PlayerController>();
            playerHiding = playerObj.GetComponent<HidingSystem>();
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerFlashlight = playerObj.GetComponent<FlashlightController>();
            if (playerHiding != null)
                playerHiding.OnHiddenChanged += HandlePlayerHiddenChanged;

            // Player and killer use distance-based attacks, so their physical
            // bodies must never push/carry each other. This also prevents the
            // player standing on the killer's head and stalling its Rigidbody.
            Collider2D[] killerColliders = GetComponentsInChildren<Collider2D>();
            Collider2D[] playerColliders = playerObj.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D killerCollider in killerColliders)
            {
                foreach (Collider2D playerCollider in playerColliders)
                {
                    if (killerCollider != null && playerCollider != null)
                        Physics2D.IgnoreCollision(killerCollider, playerCollider, true);
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            if (player == null)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            if (chaseMemoryTimer > 0f) chaseMemoryTimer -= Time.deltaTime;
            if (screamCooldownTimer > 0f) screamCooldownTimer -= Time.deltaTime;
            if (wallAvoidTimer > 0f) wallAvoidTimer -= Time.deltaTime;
            if (wallRepathTimer > 0f) wallRepathTimer -= Time.deltaTime;
            if (chaseCommitTimer > 0f) chaseCommitTimer -= Time.deltaTime;
            UpdateStuckRecovery();
            bool rawDetected = DetectPlayer();
            if (rawDetected)
                suspicion = Mathf.MoveTowards(suspicion, 1f, Time.deltaTime / Mathf.Max(0.05f, suspicionBuildTime));
            else
                suspicion = Mathf.MoveTowards(suspicion, 0f, Time.deltaTime / Mathf.Max(0.05f, suspicionFadeTime));

            bool alreadyCommitted = state == KillerState.Chase || state == KillerState.Attack ||
                state == KillerState.AttackRecover || state == KillerState.Scream;
            bool detected = rawDetected && (alreadyCommitted || suspicion >= 0.999f);

            if (rawDetected)
            {
                lostDetectionTimer = 0f;
                lastKnownPosition = player.position;
                if (isBlind) chaseMemoryTimer = BlindChaseMemory;
            }
            else if (state == KillerState.Chase)
            {
                lostDetectionTimer += Time.deltaTime;
            }

            // Level 2 petrification now builds gradually while the killer can
            // actually see the player, rather than freezing on the scream frame.
            if (freezeInsteadOfStun && playerController != null && CanSeePlayerForScream())
            {
                float distance = Vector2.Distance(transform.position, player.position);
                float closeness = 1f - Mathf.Clamp01(distance / 15f);
                // Far sight gives the player time to react; staying close remains dangerous.
                float cursePerSecond = Mathf.Lerp(10f, 22f, closeness);
                playerController.ApplyCursePressure(cursePerSecond * Time.deltaTime, this);
            }

            if (animator != null)
            {
                bool isWalking = (state == KillerState.Patrol || state == KillerState.Investigate ||
                    state == KillerState.InvestigateSound || state == KillerState.Search ||
                    state == KillerState.ReturnToPatrol) &&
                    Mathf.Abs(body.linearVelocity.x) > 0.01f;
                animator.SetBool(IsWalkingHash, isWalking);
                animator.SetBool(IsRunningHash, state == KillerState.Chase || state == KillerState.Attack);
            }

            switch (state)
            {
                case KillerState.Patrol:
                    TickPatrol(detected);
                    break;
                case KillerState.Alert:
                    TickAlert(detected);
                    break;
                case KillerState.Listening:
                    TickListening(detected);
                    break;
                case KillerState.InvestigateSound:
                    TickInvestigateSound(detected);
                    break;
                case KillerState.ConfirmSound:
                    TickConfirmSound(detected);
                    break;
                case KillerState.Confused:
                    TickConfused(detected);
                    break;
                case KillerState.Investigate:
                    TickInvestigate(detected);
                    break;
                case KillerState.Search:
                    TickSearch(detected);
                    break;
                case KillerState.Chase:
                    TickChase(detected);
                    break;
                case KillerState.Attack:
                    TickAttack(detected);
                    break;
                case KillerState.AttackRecover:
                    TickAttackRecover(detected);
                    break;
                case KillerState.ReturnToPatrol:
                    TickReturnToPatrol(detected);
                    break;
                case KillerState.Scream:
                    TickScream(detected);
                    break;
            }
        }

        private bool CanSeePlayerForScream()
        {
            if (!canScream) return false;
            if (isBlind) return false;
            if (playerHiding != null && playerHiding.IsHidden) return false;
            
            Vector2 eye = (Vector2)transform.position + Vector2.up * eyeHeight;
            Vector2 toPlayer = (Vector2)player.position + Vector2.up * eyeHeight - eye;
            float dist = toPlayer.magnitude;

            if (dist > 15f) return false;

            Vector2 facingDir = new Vector2(facing, 0f);
            float angle = Vector2.Angle(facingDir, toPlayer);
            if (angle > sightHalfAngle) return false;

            RaycastHit2D hit = Physics2D.Raycast(eye, toPlayer.normalized, dist, obstacleMask);
            return hit.collider == null;
        }

        private void TriggerScream()
        {
            if (playerController != null)
            {
                if (!freezeInsteadOfStun)
                    playerController.Stun(screamDuration);
            }
            if (animator != null)
            {
                animator.SetTrigger(ScreamHash);
            }
            screamTimer = freezeInsteadOfStun ? Mathf.Max(1.2f, screamDuration) : screamDuration;
            screamCooldownTimer = screamCooldown;
            SetState(KillerState.Scream);
        }

        /// <summary>Called when the Level 2 mirror reflects a full curse.</summary>
        public void ReflectCurse()
        {
            curseReflected = true;
            screamTimer = 2.5f;
            screamCooldownTimer = screamCooldown;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.45f, 0.9f, 1f, 1f);
            if (animator != null) animator.SetTrigger(ScreamHash);
            if (detectClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(detectClip, 0.9f, 1.45f);
            SetState(KillerState.Scream);
        }

        private bool DetectPlayer()
        {
            if (playerHiding != null && playerHiding.IsHidden)
            {
                return false;
            }

            Vector2 eye = (Vector2)transform.position + Vector2.up * eyeHeight;
            Vector2 toPlayer = (Vector2)player.position + Vector2.up * eyeHeight - eye;
            float dist = toPlayer.magnitude;

            bool flashlightOn = playerFlashlight != null && playerFlashlight.IsOn;

            // ---- Sight ----
            if (!isBlind)
            {
                float effectiveSight = flashlightOn ? sightRange * flashlightSightMultiplier : sightRange;
                if (dist <= effectiveSight)
            {
                bool inFov = flashlightOn; // flashlight makes the player visible from any angle
                if (!inFov)
                {
                    Vector2 facingDir = new Vector2(facing, 0f);
                    float angle = Vector2.Angle(facingDir, toPlayer);
                    inFov = angle <= sightHalfAngle;
                }

                if (inFov)
                {
                    RaycastHit2D hit = Physics2D.Raycast(eye, toPlayer.normalized, dist, obstacleMask);
                    if (hit.collider == null)
                    {
                        return true;
                    }
                }
            }

            }

            // ---- Hearing ----
            if (playerController != null)
            {
                // Use the real 2D distance. Comparing only X allowed a killer in
                // another vertically separated map/room to hear the player.
                float hearingDistance = Vector2.Distance(player.position, transform.position);
                float sprintHearing = isBlind ? hearingRange * 1.25f : hearingRange;
                float movementHearing = isBlind ? hearingRange * 0.75f : hearingRange * 0.5f;

                if (playerController.IsSprinting && hearingDistance <= sprintHearing)
                {
                    return true;
                }

                if (playerController.IsMoving && hearingDistance <= movementHearing)
                {
                    return true;
                }
            }

            return false;
        }

        private void TickPatrol(bool detected)
        {
            if (detected)
            {
                EnterChase();
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                MoveHorizontal(0f, patrolSpeed);
                return;
            }

            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                MoveHorizontal(0f, patrolSpeed);
                return;
            }

            Transform target = patrolPoints[patrolIndex];
            float dx = target.position.x - transform.position.x;
            if (Mathf.Abs(dx) <= waypointTolerance)
            {
                pauseTimer = waypointPause;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                return;
            }

            MoveHorizontal(Mathf.Sign(dx), patrolSpeed);
        }

        private void TickAlert(bool detected)
        {
            MoveHorizontal(0f, patrolSpeed);
            float dx = lastKnownPosition.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f)
            {
                facing = dx > 0f ? 1 : -1;
                ApplyFacing();
            }

            if (detected)
            {
                EnterChase();
                return;
            }

            alertTimer -= Time.deltaTime;
            if (alertTimer <= 0f) SetState(KillerState.Investigate);
        }

        private void TickListening(bool detected)
        {
            MoveHorizontal(0f, patrolSpeed);
            if (detected) { EnterChase(); return; }
            float dx = lastKnownPosition.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f) { facing = dx > 0f ? 1 : -1; ApplyFacing(); }
            soundStateTimer -= Time.deltaTime;
            if (soundStateTimer <= 0f) SetState(KillerState.InvestigateSound);
        }

        private void TickInvestigateSound(bool detected)
        {
            if (detected) { EnterChase(); return; }
            float dx = lastKnownPosition.x - transform.position.x;
            if (Mathf.Abs(dx) > waypointTolerance)
            {
                float speed = isBlind ? chaseSpeed * 0.82f : patrolSpeed * 1.15f;
                MoveHorizontal(Mathf.Sign(dx), speed);
                return;
            }
            MoveHorizontal(0f, patrolSpeed);
            soundStateTimer = confirmSoundDuration;
            SetState(KillerState.ConfirmSound);
        }

        private void TickConfirmSound(bool detected)
        {
            MoveHorizontal(0f, patrolSpeed);
            if (detected) { EnterChase(); return; }
            soundStateTimer -= Time.deltaTime;
            if (soundStateTimer > 0f) return;

            if (hasActiveSound && activeSound.Type == NoiseType.Rock && activeSound.RepeatedRock)
            {
                soundStateTimer = confusedDuration;
                SetState(KillerState.Confused);
            }
            else
            {
                hasActiveSound = false;
                EnterSearch();
            }
        }

        private void TickConfused(bool detected)
        {
            MoveHorizontal(0f, patrolSpeed);
            if (detected) { EnterChase(); return; }
            soundStateTimer -= Time.deltaTime;
            if (soundStateTimer > 0f) return;
            hasActiveSound = false;
            if (soundQueue.Count > 0) SelectBestSound();
            else SetState(KillerState.ReturnToPatrol);
        }

        private void TickInvestigate(bool detected)
        {
            if (suspectedHidingSpot != null)
            {
                if (detected && (playerHiding == null || !playerHiding.IsHidden))
                {
                    suspectedHidingSpot.BeginCooldown();
                    suspectedHidingSpot = null;
                    EnterChase();
                    return;
                }

                TickHidingSpotSearch();
                return;
            }

            if (detected && wallRepathTimer <= 0f)
            {
                EnterChase();
                return;
            }

            searchTimer -= Time.deltaTime;
            float dx = lastKnownPosition.x - transform.position.x;

            if (Mathf.Abs(dx) > waypointTolerance)
            {
                // A blind killer rushes toward the last noise instead of slowly
                // returning to patrol speed as soon as the player goes quiet.
                float investigateSpeed = isBlind ? chaseSpeed * 0.78f : patrolSpeed;
                MoveHorizontal(Mathf.Sign(dx), investigateSpeed);
            }
            else
            {
                MoveHorizontal(0f, patrolSpeed);
            }

            if (searchTimer <= 0f)
            {
                EnterSearch();
            }
        }

        private void EnterSearch()
        {
            searchCenter = lastKnownPosition;
            searchTimer = Mathf.Max(2.8f, searchDuration * 0.72f);
            searchSweepDirection = facing == 0 ? 1f : facing;
            SetState(KillerState.Search);
        }

        private void TickSearch(bool detected)
        {
            if (detected)
            {
                EnterChase();
                return;
            }

            searchTimer -= Time.deltaTime;
            float sweepDistance = isBlind ? 3.2f : 2.2f;
            float targetX = searchCenter.x + searchSweepDirection * sweepDistance;
            float dx = targetX - transform.position.x;
            if (Mathf.Abs(dx) <= waypointTolerance)
            {
                searchSweepDirection *= -1f;
                MoveHorizontal(0f, patrolSpeed);
            }
            else
                MoveHorizontal(Mathf.Sign(dx), patrolSpeed * 1.12f);

            if (searchTimer <= 0f) SetState(KillerState.ReturnToPatrol);
        }

        private void HandlePlayerHiddenChanged(bool hidden)
        {
            if (!hidden || player == null) return;
            if (state != KillerState.Chase && state != KillerState.Attack &&
                state != KillerState.AttackRecover && state != KillerState.Scream) return;

            HidingSpotInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (HidingSpotInteractable spot in FindObjectsByType<HidingSpotInteractable>())
            {
                if (!spot.ContainsPlayer(playerHiding)) continue;
                float distance = Vector2.Distance(transform.position, spot.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = spot;
                    nearestDistance = distance;
                }
            }

            if (nearest == null) return;
            suspectedHidingSpot = nearest;
            hidingSearchTimer = -1f;
            lastKnownPosition = nearest.transform.position;
            searchTimer = Mathf.Max(searchDuration, 4f);
            nearest.MarkSuspicious();
            SetState(KillerState.Investigate);
        }

        private void TickHidingSpotSearch()
        {
            if (suspectedHidingSpot == null) return;
            float dx = suspectedHidingSpot.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.85f)
            {
                MoveHorizontal(Mathf.Sign(dx), Mathf.Max(patrolSpeed, chaseSpeed * 0.72f));
                return;
            }

            MoveHorizontal(0f, patrolSpeed);
            if (hidingSearchTimer < 0f)
            {
                hidingSearchTimer = 1.35f;
                suspectedHidingSpot.BeginSearch();
                if (animator != null) animator.SetTrigger(Attack1Hash);
            }

            hidingSearchTimer -= Time.deltaTime;
            if (hidingSearchTimer > 0f) return;

            bool caught = playerHiding != null && playerHiding.IsHidden &&
                suspectedHidingSpot.ContainsPlayer(playerHiding);
            HidingSpotInteractable searchedSpot = suspectedHidingSpot;
            suspectedHidingSpot = null;
            hidingSearchTimer = -1f;

            if (caught)
            {
                searchedSpot.MarkCompromised();
                playerHiding.ForceReveal();
                lastKnownPosition = player.position;
                attackTimer = 0f;
                EnterChase();
            }
            else
            {
                searchedSpot.BeginCooldown();
                searchTimer = 1.5f;
                SetState(KillerState.Investigate);
            }
        }

        private void OnDestroy()
        {
            if (playerHiding != null)
                playerHiding.OnHiddenChanged -= HandlePlayerHiddenChanged;
        }

        private void TickChase(bool detected)
        {
            if (wallRepathTimer > 0f)
            {
                searchTimer = Mathf.Max(searchTimer, wallRepathTimer);
                // Keep Chase active while steering around a wall. Switching to
                // Investigate here made movement and alert audio pulse repeatedly.
                MoveHorizontal(wallAvoidDirection, Mathf.Max(patrolSpeed * 1.15f, chaseSpeed * 0.72f));
                return;
            }

            if (screamCooldownTimer <= 0f && CanSeePlayerForScream())
            {
                TriggerScream();
                return;
            }

            float dist = Vector2.Distance(player.position, transform.position);
            if (dist <= attackRange)
            {
                SetState(KillerState.Attack);
                return;
            }

            if (!detected)
            {
                if (chaseCommitTimer > 0f)
                {
                    float committedDx = lastKnownPosition.x - transform.position.x;
                    MoveHorizontal(Mathf.Abs(committedDx) > 0.05f ? Mathf.Sign(committedDx) : facing, chaseSpeed);
                    return;
                }

                // Keep committing to the last heard position briefly. Without
                // this, stopping for a single frame makes the blind killer give up.
                if (isBlind && chaseMemoryTimer > 0f)
                {
                    float rememberedDx = lastKnownPosition.x - transform.position.x;
                    MoveHorizontal(Mathf.Abs(rememberedDx) > 0.05f ? Mathf.Sign(rememberedDx) : 0f, chaseSpeed);
                    return;
                }

                // Do not abandon a chase because line of sight flickered for one frame.
                if (lostDetectionTimer < 1.2f)
                {
                    float graceDx = lastKnownPosition.x - transform.position.x;
                    MoveHorizontal(Mathf.Abs(graceDx) > 0.05f ? Mathf.Sign(graceDx) : facing, chaseSpeed);
                    return;
                }

                searchTimer = searchDuration;
                SetState(KillerState.Investigate);
                return;
            }

            float targetX = lastKnownPosition.x;
            if (playerBody != null)
            {
                float directDistance = Mathf.Abs(player.position.x - transform.position.x);
                if (directDistance > 2.5f)
                {
                    float predictionTime = Mathf.Clamp((directDistance / Mathf.Max(chaseSpeed, 0.1f)) * 0.6f, 0.15f, 0.8f);
                    targetX = player.position.x + playerBody.linearVelocity.x * predictionTime;
                }
            }

            float dx = targetX - transform.position.x;
            MoveHorizontal(Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : 0f, chaseSpeed);
        }

        private void TickAttack(bool detected)
        {
            if (playerHiding != null && playerHiding.IsHidden)
            {
                searchTimer = searchDuration;
                SetState(KillerState.Investigate);
                return;
            }

            float dist = Vector2.Distance(player.position, transform.position);
            MoveHorizontal(0f, chaseSpeed);

            // Face the player.
            facing = player.position.x >= transform.position.x ? 1 : -1;
            ApplyFacing();

            if (dist > attackRange)
            {
                SetState(detected ? KillerState.Chase : KillerState.Investigate);
                if (!detected)
                {
                    searchTimer = searchDuration;
                }
                return;
            }

            if (attackTimer <= 0f && (playerHealth == null || !playerHealth.IsInvincible))
            {
                attackTimer = attackCooldown;
                playerHealth?.TakeDamage(attackDamage);
                
                if (animator != null)
                {
                    animator.SetTrigger(Attack1Hash);
                }

                if (attackClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfxAt(attackClip, transform.position, 1f);
                }

                attackRecoverTimer = Mathf.Clamp(attackCooldown * 0.42f, 0.48f, 0.8f);
                SetState(KillerState.AttackRecover);
            }
        }

        private void TickAttackRecover(bool detected)
        {
            MoveHorizontal(0f, chaseSpeed);
            attackRecoverTimer -= Time.deltaTime;
            if (attackRecoverTimer > 0f) return;

            if (playerHiding != null && playerHiding.IsHidden)
            {
                searchTimer = searchDuration;
                SetState(KillerState.Investigate);
                return;
            }

            if (detected) SetState(KillerState.Chase);
            else EnterSearch();
        }

        private void TickReturnToPatrol(bool detected)
        {
            if (detected)
            {
                EnterChase();
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                SetState(KillerState.Patrol);
                return;
            }

            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                float distance = Mathf.Abs(patrolPoints[i].position.x - transform.position.x);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            patrolIndex = nearestIndex;
            float dx = patrolPoints[patrolIndex].position.x - transform.position.x;
            if (Mathf.Abs(dx) <= waypointTolerance)
            {
                pauseTimer = waypointPause * 0.5f;
                SetState(KillerState.Patrol);
            }
            else
            {
                MoveHorizontal(Mathf.Sign(dx), patrolSpeed);
            }
        }

        private void TickScream(bool detected)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            screamTimer -= Time.deltaTime;
            facing = player.position.x >= transform.position.x ? 1 : -1;
            ApplyFacing();
            if (screamTimer <= 0f)
            {
                if (curseReflected && spriteRenderer != null)
                    spriteRenderer.color = new Color(1f, 0.85f, 0.85f, 1f);
                curseReflected = false;
                SetState(KillerState.Chase);
            }
        }

        private void EnterChase()
        {
            suspicion = 1f;
            chaseCommitTimer = Mathf.Max(chaseCommitTimer, 1.25f);
            hasActiveSound = false;
            lastKnownPosition = player.position;
            if (state != KillerState.Chase && state != KillerState.Scream)
            {
                if (detectClip != null && AudioManager.Instance != null && Time.unscaledTime >= nextDetectSoundTime)
                {
                    AudioManager.Instance.PlaySfxAt(detectClip, transform.position, 1f);
                    nextDetectSoundTime = Time.unscaledTime + 5f;
                }

                if (screamCooldownTimer <= 0f && CanSeePlayerForScream())
                {
                    TriggerScream();
                }
                else
                {
                    SetState(KillerState.Chase);
                }
            }
            else if (state != KillerState.Scream)
            {
                SetState(KillerState.Chase);
            }
        }

        private void MoveHorizontal(float dir, float speed)
        {
            if (state == KillerState.Chase || state == KillerState.Attack)
                speed *= directorChaseMultiplier;
            if (Mathf.Abs(dir) < 0.01f)
            {
                requestedMoveDirection = 0f;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            if (wallAvoidTimer > 0f)
            {
                dir = wallAvoidDirection;
                speed = Mathf.Min(speed, patrolSpeed * 1.25f);
            }

            requestedMoveDirection = Mathf.Sign(dir);
            if (IsTallWallAhead(requestedMoveDirection))
            {
                BeginWallRecovery(requestedMoveDirection);
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            float targetX = dir * speed;
            body.linearVelocity = new Vector2(targetX, body.linearVelocity.y);
            if (!TryFollowLevel3BridgeSlope(targetX))
            {
                TryStepUp(Mathf.Sign(targetX));
            }

            if (Mathf.Abs(dir) > 0.01f)
            {
                facing = dir > 0f ? 1 : -1;
                ApplyFacing();
            }
        }

        private bool IsTallWallAhead(float direction)
        {
            if (bodyCollider == null || Mathf.Abs(direction) < 0.01f) return false;

            Bounds bounds = bodyCollider.bounds;
            float distance = Mathf.Max(0.24f, Mathf.Abs(body.linearVelocity.x) * Time.fixedDeltaTime + 0.12f);
            float sideX = bounds.center.x + direction * (bounds.extents.x + 0.015f);

            // Probe above the maximum step height. Low obstacles are handled by TryStepUp;
            // a hit here is a real wall and must never be pushed against indefinitely.
            Vector2 upperOrigin = new Vector2(sideX - direction * 0.03f,
                Mathf.Min(bounds.max.y - 0.12f, bounds.min.y + 1.2f));
            RaycastHit2D upperHit = Physics2D.Raycast(
                upperOrigin, Vector2.right * direction, distance, obstacleMask);
            return upperHit.collider != null;
        }

        private void BeginWallRecovery(float blockedDirection)
        {
            wallAvoidDirection = -Mathf.Sign(blockedDirection);
            wallAvoidTimer = Mathf.Max(wallAvoidTimer, 0.8f);
            wallRepathTimer = Mathf.Max(wallRepathTimer, 1.65f);
            chaseMemoryTimer = 0f;
            searchTimer = Mathf.Max(searchTimer, 2f);
            lastKnownPosition = (Vector2)transform.position + Vector2.right * wallAvoidDirection * 2f;

            if (state == KillerState.Patrol && patrolPoints != null && patrolPoints.Length > 0)
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            // Chase keeps its state while repathing. TickChase handles the
            // temporary steering, avoiding rapid Chase/Investigate oscillation.
        }

        private void UpdateStuckRecovery()
        {
            if (Mathf.Abs(requestedMoveDirection) < 0.01f || state == KillerState.Scream)
            {
                stuckTimer = 0f;
                lastProgressX = transform.position.x;
                return;
            }

            if (Mathf.Abs(transform.position.x - lastProgressX) >= 0.08f)
            {
                stuckTimer = 0f;
                lastProgressX = transform.position.x;
                return;
            }

            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 0.65f)
            {
                BeginWallRecovery(requestedMoveDirection);
                stuckTimer = 0f;
                lastProgressX = transform.position.x;
            }
        }

        private bool TryFollowLevel3BridgeSlope(float targetX)
        {
            if (bodyCollider == null || Mathf.Abs(targetX) < 0.01f) return false;

            Bounds bounds = bodyCollider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.12f);
            // A longer probe keeps the AI attached when it crosses from the
            // flat bridge top onto the descending right-hand slope.
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.8f);

            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit2D hit = hits[index];
                if (hit.collider == null || hit.collider.gameObject.name != "Level3_SmoothBridgeCollider")
                    continue;

                if (hit.normal.y < 0.55f || Mathf.Abs(hit.normal.x) < 0.02f)
                    return false;

                Vector2 tangent = new Vector2(hit.normal.y, -hit.normal.x);
                if (Mathf.Sign(tangent.x) != Mathf.Sign(targetX))
                    tangent = -tangent;

                float horizontalSpeed = Mathf.Abs(targetX);
                body.linearVelocity = tangent * (horizontalSpeed / Mathf.Max(Mathf.Abs(tangent.x), 0.1f));
                return true;
            }

            return false;
        }

        private void TryStepUp(float direction)
        {
            if (bodyCollider == null) return;
            if (body.linearVelocity.y < -0.1f) return;

            const float maxStepHeight = 1.05f;
            const float stepProbeDistance = 0.18f;
            Bounds bounds = bodyCollider.bounds;
            float sideX = bounds.center.x + direction * (bounds.extents.x + 0.01f);
            Vector2 lowOrigin = new Vector2(sideX - direction * 0.04f, bounds.min.y + 0.08f);
            RaycastHit2D obstacle = Physics2D.Raycast(
                lowOrigin,
                Vector2.right * direction,
                stepProbeDistance,
                obstacleMask);

            if (obstacle.collider == null) return;

            float probeX = sideX + direction * stepProbeDistance;
            Vector2 topOrigin = new Vector2(probeX, bounds.min.y + maxStepHeight + 0.08f);
            RaycastHit2D top = Physics2D.Raycast(
                topOrigin,
                Vector2.down,
                maxStepHeight + 0.08f,
                obstacleMask);

            if (top.collider == null) return;

            float rise = top.point.y - bounds.min.y;
            if (rise <= 0.02f || rise > maxStepHeight) return;

            body.position += Vector2.up * (rise + 0.02f);
        }

        private void ApplyFacing()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = facing < 0;
            }
        }

        private void SetState(KillerState newState)
        {
            if (state == newState)
            {
                return;
            }

            Debug.Log($"[KillerAI] State changed: {state} -> {newState}");
            state = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fallbackSightRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, fallbackHearingRange);
        }
    }
}
