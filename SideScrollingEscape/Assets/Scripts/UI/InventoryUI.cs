using System.Collections.Generic;
using System.Collections;
using SideScrollingEscape.Items;
using SideScrollingEscape.Data;
using SideScrollingEscape.Core;
using SideScrollingEscape.Player;
using UnityEngine;

namespace SideScrollingEscape.UI
{
    public enum InventoryState
    {
        Closed,
        Opening,
        Browsing,
        UsingItem,
        Closing
    }

    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private InventorySlotUI[] slots;
        [SerializeField] private float openDuration = 0.16f;
        [SerializeField] private float closeDuration = 0.14f;
        [SerializeField] private float itemUseLockDuration = 0.28f;

        private CanvasGroup panelCanvasGroup;
        private RectTransform panelRect;
        private PlayerController playerController;

        public static InventoryUI Instance { get; private set; }
        public InventoryState State { get; private set; } = InventoryState.Closed;
        public event System.Action<InventoryState> OnStateChanged;

        private void Awake()
        {
            Instance = this;
            playerController = FindAnyObjectByType<PlayerController>();
            if (inventoryPanel != null)
            {
                panelRect = inventoryPanel.GetComponent<RectTransform>();
                panelCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null) panelCanvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemCollected += HandleInventoryChanged;
                InventoryManager.Instance.OnKeyCountChanged += HandleKeyCountChanged;
                InventoryManager.Instance.OnInventoryChanged += RefreshUI;
            }
        }

        private void OnDestroy()
        {
            ForceClose();
            if (Instance == this) Instance = null;
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemCollected -= HandleInventoryChanged;
                InventoryManager.Instance.OnKeyCountChanged -= HandleKeyCountChanged;
                InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
            }
        }

        private void Update()
        {
            if (State != InventoryState.Closed && playerController != null &&
                playerController.State != PlayerState.UsingItem)
            {
                ForceClose();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (State != InventoryState.Closed) ForceClose();
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (State == InventoryState.Closed) StartCoroutine(OpenRoutine());
                else if (State == InventoryState.Browsing) StartCoroutine(CloseRoutine());
            }
        }

        private IEnumerator OpenRoutine()
        {
            if (inventoryPanel == null) yield break;
            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null && playerController.State != PlayerState.Normal) yield break;

            SetState(InventoryState.Opening);
            playerController?.SetInventoryState(true);
            inventoryPanel.SetActive(true);
            RefreshUI();
            SetPanelInteraction(false);
            yield return AnimatePanel(0f, 1f, 0.9f, 1f, openDuration);
            SetPanelInteraction(true);
            SetState(InventoryState.Browsing);
        }

        private IEnumerator CloseRoutine()
        {
            SetState(InventoryState.Closing);
            SetPanelInteraction(false);
            yield return AnimatePanel(1f, 0f, 1f, 0.92f, closeDuration);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            playerController?.SetInventoryState(false);
            SetState(InventoryState.Closed);
        }

        public void TryUseItem(ItemData item)
        {
            if (State == InventoryState.Browsing && item != null)
                StartCoroutine(UseItemRoutine(item));
        }

        private IEnumerator UseItemRoutine(ItemData item)
        {
            SetState(InventoryState.UsingItem);
            SetPanelInteraction(false);
            InventoryManager.Instance?.UseItem(item);
            RefreshUI();
            yield return new WaitForSecondsRealtime(itemUseLockDuration);
            SetPanelInteraction(true);
            SetState(InventoryState.Browsing);
        }

        private IEnumerator AnimatePanel(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                if (panelRect != null) panelRect.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
                yield return null;
            }
        }

        private void SetPanelInteraction(bool enabled)
        {
            if (panelCanvasGroup == null) return;
            panelCanvasGroup.interactable = enabled;
            panelCanvasGroup.blocksRaycasts = enabled;
        }

        private void ForceClose()
        {
            StopAllCoroutines();
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            playerController?.SetInventoryState(false);
            SetState(InventoryState.Closed);
        }

        public void CloseImmediately() => ForceClose();

        private void SetState(InventoryState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void HandleInventoryChanged(ItemData item)
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                RefreshUI();
            }
        }

        private void HandleKeyCountChanged(int current, int required)
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                RefreshUI();
            }
        }

        public void RefreshUI()
        {
            if (slots == null || slots.Length == 0) return;
            if (InventoryManager.Instance == null) return;

            List<ItemData> items = InventoryManager.Instance.CollectedItems;

            for (int i = 0; i < slots.Length; i++)
            {
                if (i < items.Count)
                {
                    slots[i].SetItem(items[i]);
                }
                else
                {
                    slots[i].SetItem(null);
                }
            }
        }
    }
}
