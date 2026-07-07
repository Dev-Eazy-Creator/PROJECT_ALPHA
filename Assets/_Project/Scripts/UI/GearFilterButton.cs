// A per-slot filter tab for the item grid. "All" shows everything; otherwise it shows one slot's items.
// The InventoryScreen wires the button and highlights the selected one.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAlpha
{
    public class GearFilterButton : MonoBehaviour
    {
        [Tooltip("When true this tab shows every item and ignores Slot.")]
        [SerializeField] private bool isAll;
        [SerializeField] private EquipmentSlot slot;
        [Tooltip("Friendly name shown as the grid's section header when this tab is active.")]
        [SerializeField] private string displayName;
        [SerializeField] private Button button;
        [Tooltip("Optional highlight shown while this tab is the selected one.")]
        [SerializeField] private Image highlight;
        [Tooltip("Optional slot icon (art-ready). When set it replaces the text label on this tab.")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text label;

        public bool IsAll => isAll;
        public EquipmentSlot Slot => slot;
        public string DisplayName => displayName;
        public Button Button => button;

        private void Awake() => ApplyIcon();

        public bool Matches(EquipmentSlot itemSlot) => isAll || itemSlot == slot;

        public void SetSelected(bool on)
        {
            if (highlight != null)
            {
                highlight.enabled = on;
            }
        }

        // Show the slot icon instead of the text label when a sprite is assigned; otherwise show text.
        private void ApplyIcon()
        {
            bool hasIcon = icon != null && iconImage != null;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = hasIcon;
            }
            if (label != null)
            {
                label.gameObject.SetActive(!hasIcon);
            }
        }
    }
}
