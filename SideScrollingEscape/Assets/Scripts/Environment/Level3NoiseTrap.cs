using SideScrollingEscape.Enemy;
using SideScrollingEscape.Audio;
using UnityEngine;

namespace SideScrollingEscape.Environment
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Level3NoiseTrap : MonoBehaviour
    {
        private const float Loudness = 20f;
        [SerializeField] private Sprite triggeredSprite;
        [SerializeField] private AudioClip glassBreakClip;
        private static AudioClip rattleClip;
        private static Sprite trapSprite;
        private bool triggered;

        private void Awake()
        {
            if (trapSprite == null) trapSprite = CreateTrapSprite();

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            if (renderer.sprite == null) renderer.sprite = trapSprite;
            renderer.sortingOrder = 5;

            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.35f, 0.85f);
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            AudioManager.Instance?.SetBackgroundVolumeMultiplier(0.45f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || !other.CompareTag("Player")) return;
            triggered = true;

            if (glassBreakClip == null)
            {
                if (rattleClip == null) rattleClip = CreateRattleClip();
                glassBreakClip = rattleClip;
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(glassBreakClip, 0.45f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(glassBreakClip, transform.position, 0.45f);
            }

            KillerAI[] killers = FindObjectsByType<KillerAI>();
            for (int index = 0; index < killers.Length; index++)
                killers[index].HearNoise(transform.position, Loudness, NoiseType.Glass);

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (triggeredSprite != null) renderer.sprite = triggeredSprite;
                renderer.color = Color.white;
            }

            GetComponent<Collider2D>().enabled = false;
        }

        private static Sprite CreateTrapSprite()
        {
            var texture = new Texture2D(10, 5, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                name = "Level3_NoiseTrap_Texture"
            };

            Color clear = Color.clear;
            Color metal = new Color(0.65f, 0.68f, 0.72f, 1f);
            Color shine = new Color(0.95f, 0.85f, 0.35f, 1f);
            Color dark = new Color(0.18f, 0.2f, 0.24f, 1f);

            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 10; x++)
                texture.SetPixel(x, y, clear);

            for (int x = 1; x < 9; x++) texture.SetPixel(x, 1, dark);
            for (int x = 2; x < 8; x++) texture.SetPixel(x, 2, metal);
            texture.SetPixel(3, 3, shine);
            texture.SetPixel(6, 3, shine);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 10, 5), new Vector2(0.5f, 0.2f), 16f);
        }

        private static AudioClip CreateRattleClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.55f;
            const float impactTime = 0.09f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            float[] shardTimes = { 0.09f, 0.125f, 0.17f, 0.24f, 0.36f, 0.53f };

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float sample = 0f;

                // Short reverse-like swell before the bottle suddenly breaks.
                if (time < impactTime)
                {
                    float swell = time / impactTime;
                    sample += Random.Range(-0.18f, 0.18f) * swell * swell;
                    sample += Mathf.Sin(2f * Mathf.PI * (650f + 1800f * swell) * time) * swell * 0.16f;
                }

                // Heavy transient at the exact break point.
                float impactAge = time - impactTime;
                if (impactAge >= 0f && impactAge < 0.085f)
                {
                    float impactEnvelope = 1f - impactAge / 0.085f;
                    sample += Random.Range(-1f, 1f) * impactEnvelope * 0.8f;
                    sample += Mathf.Sin(2f * Mathf.PI * 145f * impactAge) * impactEnvelope * 0.55f;
                }

                // Several short, high-pitched shard bursts make the sound read
                // as breaking glass rather than a single metallic click.
                for (int shard = 0; shard < shardTimes.Length; shard++)
                {
                    float shardTime = time - shardTimes[shard];
                    if (shardTime < 0f) continue;

                    float envelope = Mathf.Exp(-14f * shardTime);
                    float frequency = 2200f + shard * 670f;
                    sample += Mathf.Sin(2f * Mathf.PI * frequency * shardTime) * envelope * 0.3f;
                    sample += Random.Range(-0.38f, 0.38f) * envelope;
                }

                if (impactAge >= 0f)
                {
                    // Sub-bass hit plus dissonant ringing and faint delayed
                    // whispers create the ominous horror-game tail.
                    float tailEnvelope = Mathf.Exp(-2.5f * impactAge);
                    sample += Mathf.Sin(2f * Mathf.PI * 52f * impactAge) * tailEnvelope * 0.38f;
                    sample += Mathf.Sin(2f * Mathf.PI * 1310f * impactAge) * tailEnvelope * 0.1f;
                    sample += Mathf.Sin(2f * Mathf.PI * 1361f * impactAge) * tailEnvelope * 0.09f;

                    if (impactAge > 0.25f)
                    {
                        float whisperEnvelope = Mathf.Exp(-2.1f * (impactAge - 0.25f));
                        sample += Random.Range(-0.16f, 0.16f) * whisperEnvelope;
                    }

                    if (impactAge > 0.42f)
                    {
                        float echoAge = impactAge - 0.42f;
                        sample += Mathf.Sin(2f * Mathf.PI * 2470f * echoAge)
                            * Mathf.Exp(-8f * echoAge) * 0.16f;
                    }
                }

                // Boost the generated waveform itself. PlayClipAtPoint's volume
                // is already at maximum, so this makes the shatter perceptually
                // louder while the clamp prevents digital clipping.
                float boosted = sample * 2.6f;
                samples[index] = Mathf.Clamp(boosted, -0.98f, 0.98f);
            }

            AudioClip clip = AudioClip.Create("Level3_Horror_Trap_Shatter", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
