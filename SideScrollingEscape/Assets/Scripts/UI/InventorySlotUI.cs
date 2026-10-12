using SideScrollingEscape.Data;
using SideScrollingEscape.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SideScrollingEscape.UI
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        private ItemData currentItem;

        public void SetItem(ItemData item)
        {
            currentItem = item;
            if (item != null && item.Icon != null)
            {
                icon.sprite = item.Icon;
                icon.color = Color.white;
                icon.enabled = true;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (currentItem != null)
                InventoryUI.Instance?.TryUseItem(currentItem);
        }
    }
}
