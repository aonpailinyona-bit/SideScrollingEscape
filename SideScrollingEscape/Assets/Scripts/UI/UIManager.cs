using System.Collections;
using System.Collections.Generic;
using SideScrollingEscape.Core;
using SideScrollingEscape.Data;
using SideScrollingEscape.Enemy;
using SideScrollingEscape.Items;
using SideScrollingEscape.Player;
using SideScrollingEscape.Audio;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace SideScrollingEscape.UI
{
    /// <summary>
    /// In-level UI controller. Shows the HUD (keys, health, battery, hidden and
    /// danger indicators, interaction prompt) and the pause / death / level
    /// complete / game won overlays driven by <see cref="GameManager"/> state.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameWonPanel;

        [Header("HUD - Text")]
        [SerializeField] private Text levelNameText;
        [SerializeField] private Text keyCountText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text hiddenText;
        [SerializeField] private Text dangerText;
        [SerializeField] private Text stunText;

        [Header("HUD - Bars")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image batteryFill;

        private PlayerController player;
        private PlayerHealth playerHealth;
        private HidingSystem hiding;
        private FlashlightController flashlight;
        private KillerAI[] killers;
        private GameObject pauseSettingsPanel;
        private Image staminaFill;
        private Text rockCountText;
        private float staminaTargetFill = 1f;
        private float staminaFillVelocity;
        private Image curseFill;
        private Image curseOverlay;
        private Image curseVeil;
        private Text curseWarningText;
        private Text curseResultText;
        private Coroutine curseResultRoutine;
        private float curseVisualTarget;
        private float curseVisualAmount;
        private bool curseCurrentlyExposed;
        private float curseFillTarget;
        private float curseFillVelocity;
        private float curseStateVisualFloor;

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
            }

            Unsubscribe();
        }

        private void Start()
        {
            BuildPauseSettingsMenu();
            BuildStaminaBar();
            BuildRockCounter();
            BuildCurseHud();
            BindSceneReferences();
            HandleStateChanged(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Playing);

            if (GameManager.Instance != null && GameManager.Instance.CurrentLevelConfig != null && levelNameText != null)
            {
                LevelConfig cfg = GameManager.Instance.CurrentLevelConfig;
                levelNameText.text = $"{cfg.LevelName} - {cfg.DifficultyLabel}";
            }
        }

        private void BindSceneReferences()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                hiding = playerObj.GetComponent<HidingSystem>();
                flashlight = playerObj.GetComponent<FlashlightController>();
            }

            killers = FindObjectsByType<KillerAI>();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnKeyCountChanged += HandleKeyCountChanged;
                HandleKeyCountChanged(InventoryManager.Instance.KeyCount, InventoryManager.Instance.RequiredKeyCount);
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (flashlight != null)
            {
                flashlight.OnBatteryChanged += HandleBatteryChanged;
                HandleBatteryChanged(flashlight.BatteryNormalized);
            }

            if (hiding != null)
            {
                hiding.OnHiddenChanged += HandleHiddenChanged;
                HandleHiddenChanged(false);
            }

            if (player != null)
            {
                player.OnFrozenStateChanged += HandleFrozenStateChanged;
                player.OnStaminaChanged += HandleStaminaChanged;
                player.OnRockCountChanged += HandleRockCountChanged;
                player.OnCurseChanged += HandleCurseChanged;
                player.OnCurseStateChanged += HandleCurseStateChanged;
                player.OnCurseStatueResult += HandleCurseStatueResult;
                HandleStaminaChanged(player.CurrentStamina, player.MaxStamina);
                HandleRockCountChanged(player.CurrentRocks, player.MaxRocks);
                HandleCurseChanged(player.CurrentCurse, player.MaxCurse, false);
                HandleCurseStateChanged(player.CurrentCurseState);
            }

            if (killers != null)
            {
                foreach (var k in killers)
                {
                    if (k != null) k.OnStateChanged += HandleKillerStateChanged;
                }
                HandleKillerStateChanged(KillerState.Patrol);
            }
        }

        private void Unsubscribe()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnKeyCountChanged -= HandleKeyCountChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
            }

            if (flashlight != null)
            {
                flashlight.OnBatteryChanged -= HandleBatteryChanged;
            }

            if (hiding != null)
            {
                hiding.OnHiddenChanged -= HandleHiddenChanged;
            }

            if (player != null)
            {
                player.OnFrozenStateChanged -= HandleFrozenStateChanged;
                player.OnStaminaChanged -= HandleStaminaChanged;
                player.OnRockCountChanged -= HandleRockCountChanged;
                player.OnCurseChanged -= HandleCurseChanged;
                player.OnCurseStateChanged -= HandleCurseStateChanged;
                player.OnCurseStatueResult -= HandleCurseStatueResult;
            }

            if (killers != null)
            {
                foreach (var k in killers)
                {
                    if (k != null) k.OnStateChanged -= HandleKillerStateChanged;
                }
            }
        }

        private void Update()
        {
            if (promptText != null)
            {
                IInteractable interactable = player != null ? player.CurrentInteractable : null;
                promptText.text = interactable != null ? interactable.GetPrompt() : string.Empty;
            }

            // Keep the meter movement smooth even when the stamina value changes every frame.
            if (staminaFill != null)
            {
                staminaFill.fillAmount = Mathf.SmoothDamp(
                    staminaFill.fillAmount,
                    staminaTargetFill,
                    ref staminaFillVelocity,
                    0.08f,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
            }

            UpdateCurseVisuals();

            if (curseFill != null)
            {
                curseFill.fillAmount = Mathf.SmoothDamp(
                    curseFill.fillAmount,
                    curseFillTarget,
                    ref curseFillVelocity,
                    curseCurrentlyExposed ? 0.14f : 0.22f,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
            }
        }

        private void HandleStateChanged(GameState state)
        {
            SetActive(hudPanel, state == GameState.Playing || state == GameState.Paused);
            SetActive(pausePanel, state == GameState.Paused);
            SetActive(deathPanel, state == GameState.Dead);
            SetActive(levelCompletePanel, state == GameState.LevelComplete);
            SetActive(gameWonPanel, state == GameState.GameWon);
            if (state == GameState.Paused) ShowPauseMain();
        }

        private void BuildPauseSettingsMenu()
        {
            if (pausePanel == null || pausePanel.transform.Find("PauseSettingsPanel") != null) return;

            StylePauseBackground();
            Transform resume = pausePanel.transform.Find("ResumeButton");
            Transform restart = pausePanel.transform.Find("RestartButton");
            Transform mainMenu = pausePanel.transform.Find("MainMenuButton");
            Transform quit = pausePanel.transform.Find("QuitButton");
            if (quit == null) return;

            GameObject pauseCard = CreateMenuCard("PauseCard", pausePanel.transform, new Vector2(520f, 570f));
            pauseCard.transform.SetAsFirstSibling();

            Transform title = pausePanel.transform.Find("Title");
            if (title != null)
            {
                SetCenteredRect(title, new Vector2(0f, 220f), new Vector2(430f, 70f));
                Text titleText = title.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.text = "PAUSED";
                    titleText.fontSize = 44;
                    titleText.fontStyle = FontStyle.Bold;
                    titleText.color = new Color(0.9f, 0.18f, 0.16f, 1f);
                }
            }

            SetButtonPosition(resume, 90f);
            if (restart != null) restart.gameObject.SetActive(false);
            SetButtonPosition(mainMenu, -56f);
            SetButtonPosition(quit, -129f);
            SetButtonLabel(quit, "Exit Game");
            StylePauseButton(resume, false);
            StylePauseButton(mainMenu, false);
            StylePauseButton(quit, true);

            GameObject settingsButton = Instantiate(quit.gameObject, pausePanel.transform);
            settingsButton.name = "SettingsButton";
            SetButtonPosition(settingsButton.transform, 17f);
            SetButtonLabel(settingsButton.transform, "Settings");
            Button settingsButtonComponent = ReplaceClonedButton(settingsButton);
            settingsButtonComponent.onClick.AddListener(OnSettingsButton);
            StylePauseButton(settingsButton.transform, false);

            Text hint = CreateText("PauseHint", pausePanel.transform, "ESC  •  RESUME", 15, new Vector2(0f, -235f), new Vector2(400f, 32f));
            hint.color = new Color(0.58f, 0.6f, 0.66f, 1f);

            pauseSettingsPanel = CreatePanel("PauseSettingsPanel", pausePanel.transform);
            GameObject settingsCard = CreateMenuCard("SettingsCard", pauseSettingsPanel.transform, new Vector2(660f, 520f));
            settingsCard.transform.SetAsFirstSibling();
            Text settingsTitle = CreateText("SettingsTitle", pauseSettingsPanel.transform, "SETTINGS", 40, new Vector2(0f, 190f), new Vector2(500f, 60f));
            settingsTitle.fontStyle = FontStyle.Bold;
            settingsTitle.color = new Color(0.9f, 0.18f, 0.16f, 1f);
            CreateVolumeSlider("MasterVolume", "Master", 85f, AudioManager.MasterParam);
            CreateVolumeSlider("SfxVolume", "SFX", 15f, AudioManager.SfxParam);
            CreateVolumeSlider("MusicVolume", "Music", -55f, AudioManager.MusicParam);

            GameObject backButton = Instantiate(quit.gameObject, pauseSettingsPanel.transform);
            backButton.name = "SettingsBackButton";
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchoredPosition = new Vector2(0f, -155f);
            SetButtonLabel(backButton.transform, "Back");
            Button back = ReplaceClonedButton(backButton);
            back.onClick.AddListener(OnSettingsBackButton);
            StylePauseButton(backButton.transform, false);
            pauseSettingsPanel.SetActive(false);
        }

        private void StylePauseBackground()
        {
            Image background = pausePanel.GetComponent<Image>();
            if (background != null) background.color = new Color(0.005f, 0.008f, 0.015f, 0.9f);
        }

        private static GameObject CreateMenuCard(string name, Transform parent, Vector2 size)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);
            SetCenteredRect(card.transform, Vector2.zero, size);
            card.GetComponent<Image>().color = new Color(0.035f, 0.04f, 0.055f, 0.97f);
            Outline outline = card.GetComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.08f, 0.08f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            card.GetComponent<Image>().raycastTarget = false;

            GameObject accent = new GameObject("TopAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = Vector2.one;
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.offsetMin = new Vector2(0f, -4f);
            accentRect.offsetMax = Vector2.zero;
            accent.GetComponent<Image>().color = new Color(0.78f, 0.08f, 0.07f, 1f);
            accent.GetComponent<Image>().raycastTarget = false;
            return card;
        }

        private static void StylePauseButton(Transform target, bool danger)
        {
            if (target == null) return;
            SetCenteredRect(target, ((RectTransform)target).anchoredPosition, new Vector2(360f, 58f));
            Image image = target.GetComponent<Image>();
            Color normal = danger ? new Color(0.26f, 0.055f, 0.055f, 0.98f) : new Color(0.095f, 0.105f, 0.13f, 0.98f);
            if (image != null) image.color = normal;

            Outline outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = danger ? new Color(0.72f, 0.1f, 0.08f, 0.9f) : new Color(0.28f, 0.3f, 0.36f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = target.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = normal;
                colors.highlightedColor = danger ? new Color(0.62f, 0.1f, 0.08f, 1f) : new Color(0.24f, 0.26f, 0.32f, 1f);
                colors.pressedColor = new Color(0.72f, 0.12f, 0.1f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.08f, 0.08f, 0.09f, 0.5f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                button.colors = colors;
            }

            Text label = target.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 25;
                label.fontStyle = FontStyle.Bold;
                label.color = danger ? new Color(1f, 0.72f, 0.68f, 1f) : new Color(0.92f, 0.93f, 0.96f, 1f);
                label.raycastTarget = false;
            }
        }

        private static void SetCenteredRect(Transform target, Vector2 position, Vector2 size)
        {
            if (target == null) return;
            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void CreateVolumeSlider(string objectName, string label, float y, string audioParameter)
        {
            GameObject row = new GameObject(objectName, typeof(RectTransform));
            row.transform.SetParent(pauseSettingsPanel.transform, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(520f, 55f);
            rowRect.anchoredPosition = new Vector2(0f, y);

            CreateText("Label", row.transform, label, 24, new Vector2(-170f, 0f), new Vector2(150f, 45f));

            GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(row.transform, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(300f, 30f);
            sliderRect.anchoredPosition = new Vector2(80f, 0f);

            Image background = CreateSliderImage("Background", sliderObject.transform, new Color(0.12f, 0.12f, 0.15f, 1f));
            background.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;

            Image fill = CreateSliderImage("Fill", sliderObject.transform, new Color(0.75f, 0.12f, 0.12f, 1f));
            fill.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            fill.rectTransform.anchorMax = new Vector2(1f, 0.7f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            Image handle = CreateSliderImage("Handle", sliderObject.transform, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(20f, 32f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            float current = AudioManager.Instance != null ? AudioManager.Instance.GetVolume(audioParameter) : 1f;
            slider.SetValueWithoutNotify(current);
            slider.onValueChanged.AddListener(value => AudioManager.Instance?.SetVolume(audioParameter, value));
        }

        private static Image CreateSliderImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.025f, 0.025f, 0.04f, 0.97f);
            return panel;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static void SetButtonPosition(Transform button, float y)
        {
            if (button != null && button is RectTransform rect)
                rect.anchoredPosition = new Vector2(0f, y);
        }

        private static Button ReplaceClonedButton(GameObject buttonObject)
        {
            Button oldButton = buttonObject.GetComponent<Button>();
            ColorBlock colors = oldButton != null ? oldButton.colors : ColorBlock.defaultColorBlock;
            Navigation navigation = oldButton != null ? oldButton.navigation : Navigation.defaultNavigation;
            if (oldButton != null) DestroyImmediate(oldButton);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.colors = colors;
            button.navigation = navigation;
            return button;
        }

        private static void SetButtonLabel(Transform button, string label)
        {
            if (button == null) return;
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
        }

        private void ShowPauseMain()
        {
            if (pausePanel == null || pauseSettingsPanel == null) return;
            foreach (Transform child in pausePanel.transform)
                child.gameObject.SetActive(child.gameObject != pauseSettingsPanel && child.name != "RestartButton");
            pauseSettingsPanel.SetActive(false);
        }

        public void OnSettingsButton()
        {
            if (pausePanel == null || pauseSettingsPanel == null) return;
            foreach (Transform child in pausePanel.transform)
                child.gameObject.SetActive(child.gameObject == pauseSettingsPanel);
            pauseSettingsPanel.SetActive(true);
        }

        public void OnSettingsBackButton()
        {
            ShowPauseMain();
        }

        private void HandleKeyCountChanged(int current, int required)
        {
            if (keyCountText != null)
            {
                keyCountText.text = $"Keys: {current} / {required}";
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthFill != null && max > 0f)
            {
                healthFill.fillAmount = current / max;
            }
        }

        private void HandleBatteryChanged(float normalized)
        {
            if (batteryFill != null)
            {
                batteryFill.fillAmount = normalized;
            }
        }

        private void HandleStaminaChanged(float current, float maximum)
        {
            if (staminaFill != null && maximum > 0f)
            {
                staminaTargetFill = Mathf.Clamp01(current / maximum);
                staminaFill.color = current <= maximum * 0.25f
                    ? new Color(0.95f, 0.25f, 0.15f, 1f)
                    : new Color(0.2f, 0.85f, 0.35f, 1f);
            }
        }

        private void HandleRockCountChanged(int current, int maximum)
        {
            if (rockCountText != null)
                rockCountText.text = $"[Q]  Rock  {current} / {maximum}";
        }

        private void HandleCurseChanged(float current, float maximum, bool exposed)
        {
            if (curseFill == null || maximum <= 0f) return;
            float normalized = Mathf.Clamp01(current / maximum);
            curseVisualTarget = Mathf.Max(normalized, curseStateVisualFloor);
            curseCurrentlyExposed = exposed;
            curseFillTarget = normalized;
            curseFill.color = Color.Lerp(
                new Color(0.72f, 0.74f, 0.77f, 1f),
                new Color(0.19f, 0.205f, 0.23f, 1f),
                normalized);

        }

        private void HandleCurseStateChanged(CurseState state)
        {
            curseStateVisualFloor = state == CurseState.Petrified ? 1f :
                state == CurseState.Recovering ? 0.14f : 0f;
            curseVisualTarget = Mathf.Max(curseFillTarget, curseStateVisualFloor);
            if (curseWarningText == null) return;
            switch (state)
            {
                case CurseState.Safe:
                    curseWarningText.enabled = false;
                    break;
                case CurseState.Exposed:
                    curseWarningText.enabled = true;
                    curseWarningText.text = "CURSE GAZE — GET BEHIND COVER";
                    curseWarningText.color = new Color(0.78f, 0.84f, 0.92f, 1f);
                    break;
                case CurseState.Warning:
                    curseWarningText.enabled = true;
                    curseWarningText.text = "CURSE RISING — BREAK LINE OF SIGHT";
                    curseWarningText.color = new Color(0.92f, 0.68f, 0.34f, 1f);
                    break;
                case CurseState.Critical:
                    curseWarningText.enabled = true;
                    curseWarningText.text = "CURSE CRITICAL — FIND COVER NOW!";
                    curseWarningText.color = new Color(1f, 0.16f, 0.14f, 1f);
                    break;
                case CurseState.Petrified:
                    curseWarningText.enabled = true;
                    curseWarningText.text = "PETRIFIED — BREAK FREE!";
                    curseWarningText.color = new Color(0.7f, 0.72f, 0.76f, 1f);
                    break;
                case CurseState.Recovering:
                    curseWarningText.enabled = true;
                    curseWarningText.text = "CURSE RESISTANCE — RECOVERING";
                    curseWarningText.color = new Color(0.38f, 0.86f, 0.92f, 1f);
                    break;
            }
        }

        private void HandleCurseStatueResult(bool cleansed)
        {
            if (curseResultText == null) return;
            if (curseResultRoutine != null) StopCoroutine(curseResultRoutine);
            curseResultRoutine = StartCoroutine(ShowCurseResult(cleansed));
        }

        private IEnumerator ShowCurseResult(bool cleansed)
        {
            curseResultText.text = cleansed ? "CURSE CLEANSED" : "FALSE STATUE — CURSE INCREASED";
            Color baseColor = cleansed
                ? new Color(0.3f, 0.92f, 1f, 1f)
                : new Color(1f, 0.16f, 0.12f, 1f);
            curseResultText.enabled = true;
            curseResultText.rectTransform.localScale = Vector3.one * 0.82f;

            float timer = 0f;
            const float duration = 1.8f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float appear = Mathf.Clamp01(t / 0.14f);
                float fade = 1f - Mathf.Clamp01((t - 0.68f) / 0.32f);
                curseResultText.color = new Color(baseColor.r, baseColor.g, baseColor.b, appear * fade);
                curseResultText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, appear);
                yield return null;
            }

            curseResultText.enabled = false;
            curseResultRoutine = null;
        }

        private void UpdateCurseVisuals()
        {
            if (curseOverlay == null) return;

            // Ease the effect in and out so curse changes never flash abruptly.
            curseVisualAmount = Mathf.MoveTowards(
                curseVisualAmount,
                curseVisualTarget,
                Time.unscaledDeltaTime * (curseVisualTarget > curseVisualAmount ? 0.48f : 0.75f));

            bool visible = curseVisualAmount > 0.005f;
            curseOverlay.enabled = visible;
            float pulse = curseCurrentlyExposed
                ? (Mathf.Sin(Time.unscaledTime * Mathf.Lerp(2.2f, 6.5f, curseVisualAmount)) * 0.5f + 0.5f)
                : 0f;

            // The generated soft vignette has a clear centre and irregular, stone-like edges.
            float edgeAlpha = Mathf.SmoothStep(0f, 0.92f, curseVisualAmount)
                + pulse * curseVisualAmount * 0.06f;
            curseOverlay.color = new Color(0.075f, 0.085f, 0.11f, edgeAlpha);

            if (curseVeil != null)
            {
                curseVeil.enabled = visible;
                float veilAlpha = curseVisualAmount * 0.075f + pulse * curseVisualAmount * 0.018f;
                curseVeil.color = new Color(0.42f, 0.46f, 0.52f, veilAlpha);
            }

            if (curseWarningText != null && curseWarningText.enabled)
            {
                float warningPulse = 1f + pulse * Mathf.Lerp(0.015f, 0.055f, curseVisualAmount);
                curseWarningText.rectTransform.localScale = Vector3.one * warningPulse;
                Color warningColor = curseVisualAmount > 0.72f
                    ? new Color(1f, 0.12f, 0.1f, 0.82f + pulse * 0.18f)
                    : new Color(0.82f, 0.86f, 0.92f, 0.84f + pulse * 0.16f);
                curseWarningText.color = warningColor;
            }
        }

        private void BuildCurseHud()
        {
            if (hudPanel == null || SceneManager.GetActiveScene().name != "Level2") return;

            GameObject overlayObject = new GameObject("CurseStoneOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(hudPanel.transform, false);
            overlayObject.transform.SetAsFirstSibling();
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            curseOverlay = overlayObject.GetComponent<Image>();
            curseOverlay.color = Color.clear;
            curseOverlay.raycastTarget = false;
            curseOverlay.enabled = false;
            curseOverlay.sprite = CreateStoneVignetteSprite();

            GameObject veilObject = new GameObject("CurseDesaturationVeil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            veilObject.transform.SetParent(hudPanel.transform, false);
            veilObject.transform.SetSiblingIndex(1);
            RectTransform veilRect = veilObject.GetComponent<RectTransform>();
            veilRect.anchorMin = Vector2.zero;
            veilRect.anchorMax = Vector2.one;
            veilRect.offsetMin = veilRect.offsetMax = Vector2.zero;
            curseVeil = veilObject.GetComponent<Image>();
            curseVeil.color = Color.clear;
            curseVeil.raycastTarget = false;
            curseVeil.enabled = false;

            Transform staminaBar = hudPanel.transform.Find("StaminaBar");
            GameObject bar = staminaBar != null
                ? Instantiate(staminaBar.gameObject, hudPanel.transform)
                : new GameObject("CurseBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.name = "CurseBar";
            if (bar.transform.parent == null) bar.transform.SetParent(hudPanel.transform, false);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 28f);
            barRect.sizeDelta = new Vector2(320f, 22f);

            Transform fillTransform = bar.transform.Find("Fill");
            if (fillTransform == null)
            {
                GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObject.transform.SetParent(bar.transform, false);
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                fillTransform = fillObject.transform;
            }
            curseFill = fillTransform.GetComponent<Image>();
            curseFill.type = Image.Type.Filled;
            curseFill.fillMethod = Image.FillMethod.Horizontal;
            curseFill.fillOrigin = 0;
            curseFill.fillAmount = 0f;
            curseFill.raycastTarget = false;

            Transform staminaLabel = hudPanel.transform.Find("StaminaLabel");
            Text label;
            if (staminaLabel != null)
            {
                GameObject labelObject = Instantiate(staminaLabel.gameObject, hudPanel.transform);
                labelObject.name = "CurseLabel";
                label = labelObject.GetComponent<Text>();
                label.text = "CURSE";
            }
            else
            {
                label = CreateText("CurseLabel", hudPanel.transform, "CURSE", 18,
                    new Vector2(0f, 58f), new Vector2(220f, 28f));
            }
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 54f);
            labelRect.sizeDelta = new Vector2(220f, 24f);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.72f, 0.76f, 0.82f, 1f);

            curseWarningText = CreateText("CurseWarning", hudPanel.transform, "CURSE GAZE — GET BEHIND COVER", 25,
                new Vector2(0f, 155f), new Vector2(760f, 55f));
            curseWarningText.fontStyle = FontStyle.Bold;
            curseWarningText.enabled = false;
            curseWarningText.raycastTarget = false;

            curseResultText = CreateText("CurseStatueResult", hudPanel.transform, string.Empty, 31,
                new Vector2(0f, 82f), new Vector2(820f, 60f));
            curseResultText.fontStyle = FontStyle.Bold;
            curseResultText.enabled = false;
            curseResultText.raycastTarget = false;
        }

        private Sprite CreateStoneVignetteSprite()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeCurseStoneVignette",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = (y / (size - 1f) - 0.5f) * 2f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (size - 1f) - 0.5f) * 2f;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    float vignette = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1.18f, radius));
                    float grain = Mathf.PerlinNoise(x * 0.075f, y * 0.075f) * 0.22f - 0.08f;
                    float alpha = Mathf.Clamp01(vignette + grain * vignette);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildRockCounter()
        {
            if (hudPanel == null || hudPanel.transform.Find("RockCounter") != null) return;
            rockCountText = CreateText("RockCounter", hudPanel.transform, "[Q]  Rock  3 / 3", 18,
                new Vector2(0f, 0f), new Vector2(220f, 34f));
            RectTransform rect = rockCountText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 22f);
            rockCountText.alignment = TextAnchor.MiddleRight;
            rockCountText.fontStyle = FontStyle.Bold;
            rockCountText.color = new Color(0.78f, 0.8f, 0.84f, 1f);
            rockCountText.raycastTarget = false;
        }

        private void BuildStaminaBar()
        {
            if (hudPanel == null || hudPanel.transform.Find("StaminaBar") != null) return;

            Transform healthBar = hudPanel.transform.Find("HealthBar");
            Transform healthLabel = hudPanel.transform.Find("HealthLabel");
            Transform batteryBar = hudPanel.transform.Find("BatteryBar");
            Transform batteryLabel = hudPanel.transform.Find("BatteryLabel");

            // Give all three meters the same spacing and dimensions.
            SetHudRect(healthBar, new Vector2(30f, 91f), new Vector2(320f, 22f));
            SetHudRect(healthLabel, new Vector2(30f, 114f), new Vector2(120f, 24f));
            SetHudRect(batteryBar, new Vector2(30f, 5f), new Vector2(320f, 22f));
            SetHudRect(batteryLabel, new Vector2(30f, 28f), new Vector2(120f, 24f));

            GameObject bar;
            if (batteryBar != null)
            {
                // Clone an existing meter so its sprite, fill mode and border match exactly.
                bar = Instantiate(batteryBar.gameObject, hudPanel.transform);
                bar.name = "StaminaBar";
                SetHudRect(bar.transform, new Vector2(30f, 48f), new Vector2(320f, 22f));
                Transform fill = bar.transform.Find("Fill");
                staminaFill = fill != null ? fill.GetComponent<Image>() : null;
            }
            else
            {
                bar = new GameObject("StaminaBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bar.transform.SetParent(hudPanel.transform, false);
                SetHudRect(bar.transform, new Vector2(30f, 48f), new Vector2(320f, 22f));
                bar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.06f, 0.9f);

                GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObject.transform.SetParent(bar.transform, false);
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                staminaFill = fillObject.GetComponent<Image>();
                staminaFill.type = Image.Type.Filled;
                staminaFill.fillMethod = Image.FillMethod.Horizontal;
            }

            if (staminaFill != null)
            {
                staminaFill.fillOrigin = 0;
                staminaFill.fillAmount = 1f;
                staminaFill.color = new Color(0.2f, 0.85f, 0.35f, 1f);
            }

            Text label;
            if (batteryLabel != null)
            {
                GameObject labelObject = Instantiate(batteryLabel.gameObject, hudPanel.transform);
                labelObject.name = "StaminaLabel";
                SetHudRect(labelObject.transform, new Vector2(30f, 71f), new Vector2(120f, 24f));
                label = labelObject.GetComponent<Text>();
            }
            else
            {
                label = CreateText("StaminaLabel", hudPanel.transform, "Stamina", 20, Vector2.zero, new Vector2(120f, 24f));
                SetHudRect(label.transform, new Vector2(30f, 71f), new Vector2(120f, 24f));
                label.alignment = TextAnchor.MiddleLeft;
            }

            if (label != null)
            {
                label.text = "Stamina";
                label.raycastTarget = false;
            }
        }

        private static void SetHudRect(Transform target, Vector2 position, Vector2 size)
        {
            if (target == null) return;
            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void HandleHiddenChanged(bool hidden)
        {
            if (hiddenText != null)
            {
                hiddenText.enabled = hidden;
                hiddenText.text = "HIDDEN";
            }
        }

        private void HandleFrozenStateChanged(bool isFrozen)
        {
            HandleKillerStateChanged(KillerState.Patrol); // Force a refresh
        }

        private void HandleKillerStateChanged(KillerState _state)
        {
            bool anyHunting = false;
            bool anyScreaming = false;

            if (killers != null)
            {
                foreach (var k in killers)
                {
                    if (k != null)
                    {
                        if (k.State == KillerState.Chase || k.State == KillerState.Attack ||
                            k.State == KillerState.AttackRecover || k.State == KillerState.Scream) anyHunting = true;
                        if (k.State == KillerState.Scream) anyScreaming = true;
                    }
                }
            }

            if (dangerText != null)
            {
                SetActive(dangerText.gameObject, anyHunting);
                dangerText.enabled = anyHunting;
                dangerText.text = "!! DANGER !!";
                dangerText.color = Color.red;
            }

            if (stunText != null)
            {
                bool isFrozen = player != null && player.IsFrozen;
                if (isFrozen)
                {
                    SetActive(stunText.gameObject, true);
                    stunText.enabled = true;
                    stunText.text = "โดนแช่แข็ง! กด X รัวๆ!";
                    stunText.color = Color.gray;
                }
                else
                {
                    SetActive(stunText.gameObject, anyScreaming);
                    stunText.enabled = anyScreaming;
                    if (anyScreaming)
                    {
                        stunText.text = "โดนสตั๊น!";
                        stunText.color = Color.yellow;
                    }
                }
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        // ---- Button hooks ----

        /// <summary>Resumes gameplay from the pause menu.</summary>
        public void OnResumeButton()
        {
            GameManager.Instance?.ResumeGame();
        }

        /// <summary>Restarts the current level.</summary>
        public void OnRestartButton()
        {
            GameManager.Instance?.RestartLevel();
        }

        /// <summary>Returns to the main menu.</summary>
        public void OnMainMenuButton()
        {
            GameManager.Instance?.LoadMainMenu();
        }

        /// <summary>Advances to the next level after completing one.</summary>
        public void OnNextLevelButton()
        {
            GameManager.Instance?.ContinueToNextLevel();
        }

        /// <summary>Quits the game.</summary>
        public void OnQuitButton()
        {
            GameManager.Instance?.QuitGame();
        }
    }

    /// <summary>Development-only live FSM overlay and invariant checker. Toggle with Tab.</summary>
    public sealed class FsmDebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool visible;
        private float refreshTimer;
        private float validationTimer;
        private PlayerController player;
        private PlayerHealth health;
        private HidingSystem hiding;
        private KillerAI[] killers;
        private ExitDoor[] doors;
        private readonly Dictionary<string, float> lastWarnings = new Dictionary<string, float>();
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            bool gameplay = scene.name == "Level1" || scene.name == "Level2" || scene.name == "Level3";
            if (!gameplay || FindAnyObjectByType<FsmDebugOverlay>() != null) return;
            new GameObject("FSM_DebugOverlay").AddComponent<FsmDebugOverlay>();
        }

        private void Update()
        {
            bool togglePressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
            if (togglePressed || Input.GetKeyDown(KeyCode.Tab)) visible = !visible;
            refreshTimer -= Time.unscaledDeltaTime;
            validationTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f) { refreshTimer = 0.35f; RefreshReferences(); }
            if (validationTimer <= 0f) { validationTimer = 0.5f; ValidateStates(); }
        }

        private void RefreshReferences()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    health = player.GetComponent<PlayerHealth>();
                    hiding = player.GetComponent<HidingSystem>();
                }
            }
            killers = FindObjectsByType<KillerAI>();
            doors = FindObjectsByType<ExitDoor>();
        }

        private void ValidateStates()
        {
            if (player == null) return;

            if (health != null && health.IsDead && player.State != PlayerState.Dead)
            {
                Warn("dead_state", "PlayerHealth is dead but Player FSM was not Dead; repaired.", player);
                player.EnterDeadState();
            }

            if (hiding != null && hiding.IsHidden &&
                (player.State == PlayerState.Frozen || player.State == PlayerState.Dead))
            {
                Warn("hidden_invalid", "Player was hidden in an invalid state; revealed.", player);
                hiding.ForceReveal();
            }

            InventoryUI inventory = InventoryUI.Instance;
            if (inventory != null && inventory.State != InventoryState.Closed &&
                player.State != PlayerState.UsingItem)
            {
                Warn("inventory_mismatch", "Inventory was open while Player FSM rejected it; closed.", inventory);
                inventory.CloseImmediately();
            }

            foreach (KillerAI killer in killers)
            {
                if (killer != null && killer.State == KillerState.Attack && hiding != null && hiding.IsHidden)
                    Warn("attack_hidden_" + killer.GetEntityId(), killer.name + " attacked a hidden player.", killer);
            }
        }

        private void Warn(string key, string message, Object context)
        {
            if (lastWarnings.TryGetValue(key, out float last) && Time.unscaledTime - last < 3f) return;
            lastWarnings[key] = Time.unscaledTime;
            Debug.LogWarning("[FSM Validator] " + message, context);
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                labelStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
                headerStyle = new GUIStyle(labelStyle) { fontSize = 17, fontStyle = FontStyle.Bold };
                headerStyle.normal.textColor = new Color(0.35f, 1f, 0.65f);
            }

            int rows = 6 + (killers?.Length ?? 0) + (doors?.Length ?? 0);
            Rect panel = new Rect(14f, 14f, 390f, 34f + rows * 22f);
            GUI.Box(panel, GUIContent.none);
            float y = 22f;
            GUI.Label(new Rect(26f, y, 360f, 24f), "FSM DEBUG  [TAB]", headerStyle); y += 28f;
            GUI.Label(new Rect(26f, y, 360f, 22f), "Player: " + (player != null ? player.State.ToString() : "Missing"), labelStyle); y += 22f;
            GUI.Label(new Rect(26f, y, 360f, 22f), "Curse: " + (player != null ? player.CurrentCurseState + $" ({player.CurrentCurse / Mathf.Max(1f, player.MaxCurse):P0})" : "N/A"), labelStyle); y += 22f;
            GUI.Label(new Rect(26f, y, 360f, 22f), "Inventory: " + (InventoryUI.Instance != null ? InventoryUI.Instance.State.ToString() : "Missing"), labelStyle); y += 22f;
            GUI.Label(new Rect(26f, y, 360f, 22f), "Director: " + (TensionDirector.Instance != null ? TensionDirector.Instance.State + $" ({TensionDirector.Instance.Intensity01:P0})" : "Missing"), labelStyle); y += 22f;

            if (killers != null) foreach (KillerAI killer in killers)
            {
                if (killer == null) continue;
                GUI.Label(new Rect(26f, y, 360f, 22f), $"{killer.name}: {killer.State}  suspicion {killer.Suspicion01:P0}", labelStyle); y += 22f;
            }
            if (doors != null) foreach (ExitDoor door in doors)
            {
                if (door == null) continue;
                GUI.Label(new Rect(26f, y, 360f, 22f), $"{door.name}: {door.State}  unlock {door.UnlockProgress01:P0}", labelStyle); y += 22f;
            }
        }
#endif
    }

    public enum TutorialPromptState
    {
        Opening,
        Showing,
        Closing,
        Closed
    }

    /// <summary>Pauses each gameplay level on entry and explains its controls and unique mechanic.</summary>
    public sealed class TutorialPromptController : MonoBehaviour
    {
        public TutorialPromptState State { get; private set; } = TutorialPromptState.Opening;

        private PlayerController player;
        private float previousTimeScale = 1f;
        private float animationProgress;
        private string title;
        private string chapterLabel;
        private string chapterNumber;
        private string controlsText;
        private string tipsTitle;
        private string tipsText;
        private int page;
        private Color accentColor;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle hintStyle;
        private GUIStyle buttonStyle;
        private GUIStyle numberStyle;
        private Texture2D panelTexture;
        private Texture2D cardTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterTutorials()
        {
            SceneManager.sceneLoaded -= HandleTutorialSceneLoaded;
            SceneManager.sceneLoaded += HandleTutorialSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForCurrentLevel() => CreateForScene(SceneManager.GetActiveScene());

        private static void HandleTutorialSceneLoaded(Scene scene, LoadSceneMode mode) => CreateForScene(scene);

        private static void CreateForScene(Scene scene)
        {
            bool gameplay = scene.name == "Level1" || scene.name == "Level2" || scene.name == "Level3";
            if (!gameplay || FindAnyObjectByType<TutorialPromptController>() != null) return;
            new GameObject("LevelTutorialPrompt").AddComponent<TutorialPromptController>();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            player?.BeginScriptedAction();
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            BuildLevelText(SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            if (State == TutorialPromptState.Opening)
            {
                animationProgress = Mathf.MoveTowards(animationProgress, 1f, Time.unscaledDeltaTime * 5f);
                if (animationProgress >= 1f) State = TutorialPromptState.Showing;
                return;
            }

            if (State == TutorialPromptState.Showing && Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                Advance();
            }

            if (State == TutorialPromptState.Closing)
            {
                animationProgress = Mathf.MoveTowards(animationProgress, 0f, Time.unscaledDeltaTime * 7f);
                if (animationProgress <= 0f) FinishClose();
            }
        }

        private void BuildLevelText(string sceneName)
        {
            controlsText =
                "A / D        เดิน\nShift        วิ่ง (ใช้ Stamina)\n" +
                "Space        กระโดด\nE            โต้ตอบ / เปิดประตู\n" +
                "F            ไฟฉาย\nH            ซ่อนตัว\nC            กระเป๋า";

            if (sceneName == "Level1")
            {
                title = "THE FORSAKEN CELLAR";
                chapterNumber = "01";
                chapterLabel = "CHAPTER I   •   ห้องใต้ดินที่ถูกลืม";
                tipsTitle = "หลบหนีจากห้องใต้ดิน";
                tipsText = "เก็บกุญแจให้ครบ 5 ดอก แล้วหนีทางประตูออก\n" +
                    "• หลบสายตา Killer และใช้ที่ซ่อน\n• เก็บ Stamina ไว้ใช้ตอนหนี\n" +
                    "• ระวัง Jumpscare ขณะเก็บกุญแจ";
                accentColor = new Color(0.72f, 0.055f, 0.045f);
            }
            else if (sceneName == "Level2")
            {
                title = "THE STONE GAZE";
                chapterNumber = "02";
                chapterLabel = "CHAPTER II   •   สายตาแห่งศิลา";
                tipsTitle = "เอาชีวิตรอดจากคำสาป";
                tipsText = "เก็บกุญแจให้ครบ โดยอย่าให้ CURSE เต็ม\n" +
                    "• หลบสายตา Killer เพื่อหยุดคำสาป\n• กระจกสะท้อนคำสาปกลับ\n" +
                    "• รูปปั้นจริงช่วยล้างคำสาป แต่บางอันเป็นของปลอม\n" +
                    "• กลายเป็นหิน: กด X หรือ E รัว ๆ";
                accentColor = new Color(0.58f, 0.035f, 0.075f);
            }
            else
            {
                title = "ECHOES OF THE BLIND";
                chapterNumber = "03";
                chapterLabel = "CHAPTER III   •   เสียงสะท้อนของผู้ล่า";
                tipsTitle = "Killer ตาบอด — แต่มันได้ยินทุกอย่าง";
                tipsText = "เก็บกุญแจให้ครบ และควบคุมเสียงของตัวเอง\n" +
                    "• เดินเสียงเบา แต่วิ่งและเหยียบขวดเสียงดัง\n• กด Q โยนหินล่อ Killer\n" +
                    "• อย่าโยนซ้ำจุดเดิม เพราะ Killer จะเรียนรู้\n" +
                    "• ใช้ทางต่างระดับและที่ซ่อนเพื่อหลบหนี";
                accentColor = new Color(0.82f, 0.075f, 0.035f);
            }
        }

        private void Advance()
        {
            if (State != TutorialPromptState.Showing) return;
            if (page == 0) page = 1;
            else BeginClose();
        }

        private void BeginClose()
        {
            if (State == TutorialPromptState.Showing) State = TutorialPromptState.Closing;
        }

        private void FinishClose()
        {
            if (State == TutorialPromptState.Closed) return;
            State = TutorialPromptState.Closed;
            Time.timeScale = previousTimeScale;
            player?.EndScriptedAction();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (State != TutorialPromptState.Closed)
            {
                Time.timeScale = previousTimeScale;
                player?.EndScriptedAction();
            }
            if (panelTexture != null) Destroy(panelTexture);
            if (cardTexture != null) Destroy(cardTexture);
            if (buttonTexture != null) Destroy(buttonTexture);
            if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
        }

        private void BuildStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = new Color(1f, 0.9f, 0.88f);
            sectionStyle = new GUIStyle(titleStyle) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            sectionStyle.normal.textColor = new Color(0.97f, 0.82f, 0.8f);
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };
            bodyStyle.normal.textColor = new Color(0.92f, 0.88f, 0.88f);
            hintStyle = new GUIStyle(bodyStyle) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            hintStyle.normal.textColor = new Color(0.68f, 0.49f, 0.49f);
            panelTexture = MakeTexture(new Color(0.055f, 0.018f, 0.022f, 0.99f));
            cardTexture = MakeTexture(new Color(0.105f, 0.028f, 0.034f, 0.98f));
            buttonTexture = MakeTexture(Color.Lerp(accentColor, Color.black, 0.18f));
            buttonHoverTexture = MakeTexture(Color.Lerp(accentColor, Color.white, 0.12f));
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(0, 0, 0, 0)
            };
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.hover.background = buttonHoverTexture;
            buttonStyle.active.background = buttonHoverTexture;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            numberStyle = new GUIStyle(titleStyle) { fontSize = 52, alignment = TextAnchor.UpperRight };
            numberStyle.normal.textColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f);
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (State == TutorialPromptState.Closed) return;
            BuildStyles();
            Color oldColor = GUI.color;
            GUI.color = new Color(0.07f, 0f, 0.008f, animationProgress * 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            float width = Mathf.Min(620f, Screen.width - 24f);
            float height = Mathf.Min(430f, Screen.height - 24f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f,
                width, height);
            float scale = Mathf.Lerp(0.9f, 1f, animationProgress);
            panel = new Rect(panel.center.x - panel.width * scale * 0.5f,
                panel.center.y - panel.height * scale * 0.5f, panel.width * scale, panel.height * scale);
            GUI.color = new Color(0f, 0f, 0f, animationProgress * 0.72f);
            GUI.DrawTexture(new Rect(panel.x + 12f, panel.y + 14f, panel.width, panel.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, animationProgress);
            GUI.DrawTexture(panel, panelTexture);

            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, animationProgress);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 6f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, animationProgress);
            GUI.Label(new Rect(panel.x + 38f, panel.y + 20f, panel.width - 125f, 20f), chapterLabel, hintStyle);
            GUI.Label(new Rect(panel.x + 38f, panel.y + 39f, panel.width - 125f, 34f), title, titleStyle);
            GUI.Label(new Rect(panel.xMax - 122f, panel.y + 10f, 90f, 66f), chapterNumber, numberStyle);

            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, animationProgress);
            GUI.DrawTexture(new Rect(panel.x + 38f, panel.y + 82f, 68f, 2f), Texture2D.whiteTexture);
            GUI.color = new Color(0.3f, 0.09f, 0.1f, animationProgress);
            GUI.DrawTexture(new Rect(panel.x + 106f, panel.y + 82f, panel.width - 144f, 1f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, animationProgress);

            string sectionTitle = page == 0 ? "ปุ่มควบคุม" : tipsTitle;
            string pageText = page == 0 ? controlsText : tipsText;
            Rect contentCard = new Rect(panel.x + 38f, panel.y + 101f, panel.width - 76f, panel.height - 182f);
            GUI.color = new Color(1f, 1f, 1f, animationProgress);
            GUI.DrawTexture(contentCard, cardTexture);
            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, animationProgress * 0.78f);
            GUI.DrawTexture(new Rect(contentCard.x, contentCard.y, 4f, contentCard.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, animationProgress);
            GUI.Label(new Rect(contentCard.x + 24f, contentCard.y + 12f, contentCard.width - 48f, 27f), sectionTitle, sectionStyle);
            GUI.color = new Color(0.32f, 0.075f, 0.09f, animationProgress);
            GUI.DrawTexture(new Rect(contentCard.x + 24f, contentCard.y + 44f, contentCard.width - 48f, 1f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, animationProgress);
            GUI.Label(new Rect(contentCard.x + 30f, contentCard.y + 54f, contentCard.width - 60f,
                contentCard.height - 61f), pageText, bodyStyle);

            GUI.Label(new Rect(panel.center.x - 40f, panel.yMax - 73f, 80f, 18f), page == 0 ? "●  ○" : "○  ●", hintStyle);
            Rect button = new Rect(panel.center.x - 90f, panel.yMax - 52f, 180f, 34f);
            if (GUI.Button(button, page == 0 ? "ถัดไป" : "เริ่มเล่น", buttonStyle)) Advance();
            GUI.color = oldColor;
        }
    }
}
