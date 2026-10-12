using System;
using System.Collections.Generic;
using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using SideScrollingEscape.Data;
using SideScrollingEscape.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SideScrollingEscape.Items
{
    public enum JumpscareState
    {
        Dormant,
        Armed,
        Warning,
        Triggered,
        Recovering,
        Disabled
    }

    /// <summary>
    /// Per-scene manager that tracks the key items collected by the player and
    /// reports progress toward the level's escape requirement.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Tooltip("Fallback required key count if no LevelConfig is available.")]
        [SerializeField] private int fallbackRequiredKeyCount = 5;

        [Header("Level 1 Jumpscare (JP1 / JP2 / JP3)")]
        [SerializeField] private Sprite[] level1JumpscareSprites;
        [SerializeField] private AudioClip level1JumpscareClip;
        [Range(0f, 1f)] [SerializeField] private float level1JumpscareChance = 0.4f;
        [SerializeField] private int maxLevel1JumpscaresPerRun = 2;

        private readonly List<ItemData> collectedItems = new List<ItemData>();
        private PlayerController activeJumpscarePlayer;
        private int level1JumpscareCount;
        public JumpscareState CurrentJumpscareState { get; private set; } = JumpscareState.Dormant;
        public event Action<JumpscareState> OnJumpscareStateChanged;

        /// <summary>List of all collected items currently in the inventory.</summary>
        public List<ItemData> CollectedItems => collectedItems;

        /// <summary>Active instance for the current scene.</summary>
        public static InventoryManager Instance { get; private set; }

        /// <summary>Raised when the collected key count changes (current, required).</summary>
        public event Action<int, int> OnKeyCountChanged;

        /// <summary>Raised when any item is collected.</summary>
        public event Action<ItemData> OnItemCollected;
        public event Action OnInventoryChanged;

        /// <summary>Raised once all required keys have been collected.</summary>
        public event Action OnAllKeysCollected;

        /// <summary>Number of key items collected so far.</summary>
        public int KeyCount { get; private set; }

        /// <summary>Number of key items required to escape the level.</summary>
        public int RequiredKeyCount { get; private set; }

        /// <summary>True when the player has collected every required key.</summary>
        public bool HasAllKeys => KeyCount >= RequiredKeyCount;

        private void Awake()
        {
            Instance = this;

            LevelConfig config = GameManager.Instance != null ? GameManager.Instance.CurrentLevelConfig : null;
            RequiredKeyCount = config != null ? config.RequiredKeyCount : fallbackRequiredKeyCount;
        }

        private void Start()
        {
            // Broadcast the initial state so UI can initialise.
            OnKeyCountChanged?.Invoke(KeyCount, RequiredKeyCount);
        }

        private void OnDestroy()
        {
            activeJumpscarePlayer?.EndScriptedAction();
            activeJumpscarePlayer = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Adds an item to the inventory, updating the key count if applicable.
        /// </summary>
        /// <param name="item">The item data being collected.</param>
        public void AddItem(ItemData item)
        {
            if (item == null)
            {
                return;
            }

            collectedItems.Add(item);
            OnItemCollected?.Invoke(item);
            OnInventoryChanged?.Invoke();

            if (item.IsKeyItem)
            {
                KeyCount++;
                OnKeyCountChanged?.Invoke(KeyCount, RequiredKeyCount);
                TryTriggerLevel1Jumpscare();

                if (HasAllKeys)
                {
                    OnAllKeysCollected?.Invoke();
                }
            }
        }

        private void TryTriggerLevel1Jumpscare()
        {
            if (CurrentJumpscareState == JumpscareState.Disabled ||
                CurrentJumpscareState != JumpscareState.Dormant ||
                SceneManager.GetActiveScene().name != "Level1") return;
            if (level1JumpscareSprites == null || level1JumpscareSprites.Length == 0) return;
            SetJumpscareState(JumpscareState.Armed);
            if (UnityEngine.Random.value > level1JumpscareChance)
            {
                SetJumpscareState(JumpscareState.Dormant);
                return;
            }

            StartCoroutine(PlayJumpscare());
        }

        private System.Collections.IEnumerator PlayJumpscare()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            PlayerController controller = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
            if (controller != null && !controller.BeginScriptedAction())
            {
                SetJumpscareState(JumpscareState.Dormant);
                yield break;
            }
            activeJumpscarePlayer = controller;

            SetJumpscareState(JumpscareState.Warning);
            if (level1JumpscareClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(level1JumpscareClip, 0.16f, 0.68f);
            yield return new WaitForSecondsRealtime(0.2f);
            SetJumpscareState(JumpscareState.Triggered);
            level1JumpscareCount++;

            if (level1JumpscareClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(level1JumpscareClip, 0.92f, UnityEngine.Random.Range(0.96f, 1.04f));

            GameObject canvasObject = new GameObject("Level1_JumpscareCanvas", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject blackout = new GameObject("Blackout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blackout.transform.SetParent(canvasObject.transform, false);
            RectTransform blackRect = blackout.GetComponent<RectTransform>();
            blackRect.anchorMin = Vector2.zero;
            blackRect.anchorMax = Vector2.one;
            blackRect.offsetMin = blackRect.offsetMax = Vector2.zero;
            Image blackImage = blackout.GetComponent<Image>();
            blackImage.color = Color.black;
            blackImage.raycastTarget = false;

            GameObject faceObject = new GameObject("JumpscareFace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            faceObject.transform.SetParent(canvasObject.transform, false);
            RectTransform faceRect = faceObject.GetComponent<RectTransform>();
            faceRect.anchorMin = faceRect.anchorMax = new Vector2(0.5f, 0.5f);
            faceRect.sizeDelta = new Vector2(760f, 1140f);
            Image face = faceObject.GetComponent<Image>();
            face.sprite = level1JumpscareSprites[UnityEngine.Random.Range(0, level1JumpscareSprites.Length)];
            face.preserveAspect = true;
            face.raycastTarget = false;

            const float duration = 0.72f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float zoom = Mathf.Lerp(0.56f, 1.34f, 1f - Mathf.Pow(1f - progress, 3f));
                faceRect.localScale = Vector3.one * zoom;
                float shake = Mathf.Lerp(28f, 4f, progress);
                faceRect.anchoredPosition = UnityEngine.Random.insideUnitCircle * shake;
                yield return null;
            }

            Destroy(canvasObject);
            SetJumpscareState(JumpscareState.Recovering);
            yield return new WaitForSecondsRealtime(0.35f);
            controller?.EndScriptedAction();
            activeJumpscarePlayer = null;
            SetJumpscareState(level1JumpscareCount >= Mathf.Max(1, maxLevel1JumpscaresPerRun)
                ? JumpscareState.Disabled
                : JumpscareState.Dormant);
        }

        private void SetJumpscareState(JumpscareState newState)
        {
            if (CurrentJumpscareState == newState) return;
            CurrentJumpscareState = newState;
            OnJumpscareStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Removes an item from the inventory.
        /// </summary>
        /// <param name="item">The item data to remove.</param>
        public bool RemoveItem(ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            bool removed = collectedItems.Remove(item);
            if (removed)
            {
                if (item.IsKeyItem)
                {
                    KeyCount = Mathf.Max(0, KeyCount - 1);
                }
                OnKeyCountChanged?.Invoke(KeyCount, RequiredKeyCount);
                OnInventoryChanged?.Invoke();
            }
            return removed;
        }

        /// <summary>Uses a consumable item from the inventory.</summary>
        public bool UseItem(ItemData item)
        {
            if (item == null || !collectedItems.Contains(item)) return false;
            if (item.ItemId != "heal_20") return false;

            GameObject playerObject = GameObject.FindWithTag("Player");
            PlayerHealth health = playerObject != null ? playerObject.GetComponent<PlayerHealth>() : null;
            if (health == null || health.IsDead || health.CurrentHealth >= health.MaxHealth - 0.01f) return false;

            health.Heal(20f);
            HealingPickup.PlayUseSound();
            return RemoveItem(item);
        }

        /// <summary>Consumes the first inventory item matching an id.</summary>
        public bool TryConsumeItemById(string itemId)
        {
            ItemData item = collectedItems.Find(candidate => candidate != null && candidate.ItemId == itemId);
            return item != null && RemoveItem(item);
        }

        /// <summary>
        /// Returns true if an item with the given id has been collected.
        /// </summary>
        /// <param name="itemId">The item id to look for.</param>
        public bool HasItem(string itemId)
        {
            return collectedItems.Exists(i => i != null && i.ItemId == itemId);
        }
    }
}
