using SideScrollingEscape.Audio;
using SideScrollingEscape.Data;
using UnityEngine;

namespace SideScrollingEscape.Items
{
    /// <summary>
    /// A collectible placed in the world. When the player touches it, its
    /// <see cref="ItemData"/> is added to the <see cref="InventoryManager"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [Tooltip("The item granted when this pickup is collected.")]
        [SerializeField] private ItemData itemData;

        [Tooltip("Tag of the collector (usually the player).")]
        [SerializeField] private string collectorTag = "Player";

        [Header("Visuals")]
        [Tooltip("Optional bobbing animation amplitude (world units).")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float spinSpeed = 0f;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupClip;

        private Vector3 startPosition;
        private bool collected;

        /// <summary>The item data this pickup grants.</summary>
        public ItemData ItemData => itemData;

        /// <summary>Moves a pickup while also updating the centre of its bobbing animation.</summary>
        public void SetSpawnPosition(Vector3 position)
        {
            transform.position = position;
            startPosition = position;
        }

        private void Awake()
        {
            startPosition = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            if (bobAmplitude > 0f)
            {
                float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                transform.position = startPosition + new Vector3(0f, y, 0f);
            }

            if (spinSpeed != 0f)
            {
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag(collectorTag))
            {
                return;
            }

            Collect();
        }

        /// <summary>
        /// Collects the item, adds it to the inventory and removes the pickup.
        /// </summary>
        public void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(itemData);
            }

            if (pickupClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxAt(pickupClip, transform.position, 0.9f);
            }

            Destroy(gameObject);
        }
    }
}
