using SideScrollingEscape.Audio;
using SideScrollingEscape.Enemy;
using UnityEngine;

namespace SideScrollingEscape.Player
{
    /// <summary>A small throwable distraction that alerts nearby killers on impact.</summary>
    public sealed class ThrownRock : MonoBehaviour
    {
        private static Sprite rockSprite;
        private static AudioClip impactClip;
        private Rigidbody2D body;
        private bool impacted;

        public void Launch(Collider2D ownerCollider, int direction)
        {
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetRockSprite();
            renderer.color = new Color(0.48f, 0.5f, 0.54f, 1f);
            renderer.sortingOrder = 20;

            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.19f;
            if (ownerCollider != null) Physics2D.IgnoreCollision(collider, ownerCollider, true);

            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.4f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = new Vector2(direction * 9f, 4.8f);
            body.angularVelocity = -direction * 620f;
            Destroy(gameObject, 5f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (impacted || collision.collider.CompareTag("Player")) return;
            impacted = true;

            KillerAI[] killers = FindObjectsByType<KillerAI>();
            foreach (KillerAI killer in killers)
            {
                if (killer != null) killer.HearNoise(transform.position, 1.5f, NoiseType.Rock);
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxAt(GetImpactClip(), transform.position, 0.72f);

            if (body != null)
            {
                body.linearVelocity *= 0.28f;
                body.angularVelocity *= 0.35f;
            }
            Destroy(gameObject, 1.5f);
        }

        private static Sprite GetRockSprite()
        {
            if (rockSprite != null) return rockSprite;
            Texture2D texture = new Texture2D(12, 10, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRockTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = Color.clear;
            Color stone = Color.white;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                float nx = (x - 5.5f) / 5.2f;
                float ny = (y - 4.2f) / 3.7f;
                bool inside = nx * nx + ny * ny < 1f && !(x < 2 && y > 6);
                texture.SetPixel(x, y, inside ? stone : clear);
            }
            texture.Apply();
            rockSprite = Sprite.Create(texture, new Rect(0, 0, 12, 10), new Vector2(0.5f, 0.5f), 24f);
            rockSprite.name = "RuntimeRockSprite";
            return rockSprite;
        }

        private static AudioClip GetImpactClip()
        {
            if (impactClip != null) return impactClip;
            const int rate = 22050;
            const int count = 3300;
            float[] samples = new float[count];
            uint noise = 912367u;
            for (int i = 0; i < count; i++)
            {
                noise = noise * 1664525u + 1013904223u;
                float random = ((noise >> 8) / 8388607.5f) - 1f;
                float t = (float)i / rate;
                float envelope = Mathf.Exp(-28f * t);
                samples[i] = (random * 0.55f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.45f) * envelope;
            }
            impactClip = AudioClip.Create("RockImpact", count, 1, rate, false);
            impactClip.SetData(samples, 0);
            return impactClip;
        }
    }
}
