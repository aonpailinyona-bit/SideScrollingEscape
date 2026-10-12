using UnityEngine;

namespace SideScrollingEscape.Data
{
    /// <summary>
    /// ScriptableObject describing a collectible item (typically a key item the
    /// player must gather to unlock the exit door).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "SideScrollingEscape/Item Data", order = 0)]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Unique identifier used to track this item in the inventory.")]
        [SerializeField] private string itemId = "key";

        [Tooltip("Human readable name shown in UI.")]
        [SerializeField] private string displayName = "Key";

        [Tooltip("Short flavour/description text.")]
        [TextArea]
        [SerializeField] private string description = "";

        [Tooltip("Sprite shown in inventory / HUD.")]
        [SerializeField] private Sprite icon;

        [Tooltip("If true this item counts toward the required key total for the level.")]
        [SerializeField] private bool isKeyItem = true;

        /// <summary>Unique identifier used to track this item in the inventory.</summary>
        public string ItemId => itemId;

        /// <summary>Human readable name shown in UI.</summary>
        public string DisplayName => displayName;

        /// <summary>Short flavour/description text.</summary>
        public string Description => description;

        /// <summary>Sprite shown in inventory / HUD.</summary>
        public Sprite Icon => icon;

        /// <summary>If true this item counts toward the required key total for the level.</summary>
        public bool IsKeyItem => isKeyItem;
    }
}
