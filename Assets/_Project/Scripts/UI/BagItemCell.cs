// A grid cell for one item: icon (or generated placeholder) + name + an "equipped" badge.
// Hover (mouse) or focus (controller) raises onFocus so the detail panel can inspect it; clicking the
// cell (mouse) or pressing Submit/A while it's focused raises onEquip. The Button's own colour
// transition provides the hover/focus highlight.
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjectAlpha
{
    public class BagItemCell : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [Tooltip("Shown when this item is currently equipped.")]
        [SerializeField] private GameObject equippedBadge;

        private ItemInstance item;
        private Action<ItemInstance> onFocus;
        private Action<ItemInstance> onEquip;

        public ItemInstance Item => item;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleEquip);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleEquip);
            }
        }

        public void Bind(ItemInstance instance, bool isEquipped, Action<ItemInstance> focusHandler, Action<ItemInstance> equipHandler)
        {
            item = instance;
            onFocus = focusHandler;
            onEquip = equipHandler;

            string name = instance != null && instance.Data != null ? instance.Data.DisplayName : "(none)";
            if (label != null)
            {
                label.text = instance != null && instance.UpgradeLevel > 0 ? $"{name} +{instance.UpgradeLevel}" : name;
            }

            if (icon != null)
            {
                Sprite sprite = instance != null && instance.Data != null ? instance.Data.Icon : null;
                if (sprite == null && instance != null && instance.Data != null)
                {
                    sprite = PlaceholderIcon.For(instance.Data.DisplayName);
                }
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.enabled = sprite != null;
            }

            if (equippedBadge != null)
            {
                equippedBadge.SetActive(isEquipped);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => onFocus?.Invoke(item);
        public void OnSelect(BaseEventData eventData) => onFocus?.Invoke(item);
        private void HandleEquip() => onEquip?.Invoke(item);
    }
}
