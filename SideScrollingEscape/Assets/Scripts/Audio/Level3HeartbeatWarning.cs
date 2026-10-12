using SideScrollingEscape.Core;
using SideScrollingEscape.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SideScrollingEscape.Audio
{
    /// <summary>Per-level proximity warning: procedural heartbeat and a subtle red screen pulse.</summary>
    public sealed class Level3HeartbeatWarning : MonoBehaviour
    {
        private const float WarningRange = 14f;
        private Transform player;
        private KillerAI[] killers;
        private Image vignette;
        private AudioClip heartbeatClip;
        private float intensity;
        private float nextBeatTime;
        private float refreshTimer;
        private float visualPulse;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool isGameplayLevel = scene.name == "Level1" || scene.name == "Level2" || scene.name == "Level3";
            if (!isGameplayLevel || FindAnyObjectByType<Level3HeartbeatWarning>() != null) return;
            new GameObject("HeartbeatWarning").AddComponent<Level3HeartbeatWarning>();
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
            killers = FindObjectsByType<KillerAI>();
            heartbeatClip = BuildHeartbeatClip();
            BuildVignette();
        }

        private void Update()
        {
            bool playing = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;
            if (!playing || player == null)
            {
                intensity = Mathf.MoveTowards(intensity, 0f, Time.unscaledDeltaTime * 3f);
                UpdateVignette();
                return;
            }

            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.15f;
                RefreshIntensity();
            }

            visualPulse = Mathf.MoveTowards(visualPulse, 0f, Time.unscaledDeltaTime * 2.8f);
            if (intensity > 0.035f && Time.unscaledTime >= nextBeatTime)
            {
                float interval = Mathf.Lerp(1.35f, 0.46f, intensity);
                nextBeatTime = Time.unscaledTime + interval;
                visualPulse = Mathf.Lerp(0.22f, 1f, intensity);

                if (AudioManager.Instance != null)
                {
                    float volume = Mathf.Lerp(0.14f, 0.72f, intensity);
                    float pitch = Mathf.Lerp(0.9f, 1.08f, intensity);
                    AudioManager.Instance.PlaySfx(heartbeatClip, volume, pitch);
                }
            }

            UpdateVignette();
        }

        private void RefreshIntensity()
        {
            if (killers == null || killers.Length == 0)
                killers = FindObjectsByType<KillerAI>();

            float nearest = float.PositiveInfinity;
            bool hunting = false;
            foreach (KillerAI killer in killers)
            {
                if (killer == null) continue;
                float distance = Vector2.Distance(player.position, killer.transform.position);
                if (distance < nearest)
                {
                    nearest = distance;
                    hunting = killer.IsHunting;
                }
            }

            float target = nearest < WarningRange ? 1f - Mathf.Clamp01(nearest / WarningRange) : 0f;
            if (hunting && target > 0f) target = Mathf.Clamp01(target + 0.14f);
            if (TensionDirector.Instance != null)
                target = Mathf.Max(target, TensionDirector.Instance.Intensity01 * 0.82f);
            intensity = Mathf.MoveTowards(intensity, target, 0.13f);
        }

        private void UpdateVignette()
        {
            if (vignette == null) return;
            float alpha = intensity * (0.09f + visualPulse * 0.25f);
            vignette.color = new Color(0.72f, 0.015f, 0.01f, alpha);
            vignette.enabled = alpha > 0.004f;
        }

        private void BuildVignette()
        {
            GameObject canvasObject = new GameObject("HeartbeatWarningCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject imageObject = new GameObject("DangerVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            vignette = imageObject.GetComponent<Image>();
            vignette.sprite = BuildVignetteSprite();
            vignette.type = Image.Type.Simple;
            vignette.raycastTarget = false;
            vignette.enabled = false;
        }

        private static Sprite BuildVignetteSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DangerVignette",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float edgeX = Mathf.Min(x, size - 1 - x) / 11f;
                float edgeY = Mathf.Min(y, size - 1 - y) / 11f;
                float edge = Mathf.Min(edgeX, edgeY);
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, edge);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static AudioClip BuildHeartbeatClip()
        {
            const int rate = 22050;
            const float duration = 0.42f;
            int count = Mathf.CeilToInt(rate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float first = Thump(t, 0f, 62f, 20f);
                float second = Thump(t, 0.19f, 54f, 24f) * 0.7f;
                samples[i] = Mathf.Clamp((first + second) * 0.9f, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create("HeartbeatWarning", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Thump(float time, float start, float frequency, float decay)
        {
            float t = time - start;
            if (t < 0f) return 0f;
            float low = Mathf.Sin(2f * Mathf.PI * frequency * t);
            float body = Mathf.Sin(2f * Mathf.PI * frequency * 0.48f * t) * 0.35f;
            return (low + body) * Mathf.Exp(-decay * t);
        }
    }
}
