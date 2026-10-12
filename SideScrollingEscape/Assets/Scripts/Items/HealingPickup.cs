using SideScrollingEscape.Audio;
using SideScrollingEscape.Data;
using UnityEngine;

namespace SideScrollingEscape.Items
{
    /// <summary>Adds a healing consumable to inventory on contact.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HealingPickup : MonoBehaviour
    {
        [SerializeField] private ItemData itemData;
        private Vector3 startPosition;
        private bool collected;
        private static AudioClip healClip;

        private void Awake()
        {
            startPosition = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * 2.4f + transform.position.x) * 0.1f;
            transform.position = startPosition + Vector3.up * bob;
            transform.Rotate(0f, 0f, 28f * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag("Player")) return;
            collected = true;
            InventoryManager.Instance?.AddItem(itemData);
            Destroy(gameObject);
        }

        public static void PlayUseSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(GetHealClip(), 0.7f, 1f);
        }

        private static AudioClip GetHealClip()
        {
            if (healClip != null) return healClip;
            const int rate = 22050;
            const float duration = 0.42f;
            int count = Mathf.RoundToInt(rate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                float tone = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.38f;
                tone += Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.24f;
                samples[i] = tone * envelope;
            }
            healClip = AudioClip.Create("HealingPickup", count, 1, rate, false);
            healClip.SetData(samples, 0);
            return healClip;
        }
    }
}
