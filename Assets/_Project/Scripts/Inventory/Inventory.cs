// The player's owned, UNEQUIPPED items (the bag). Equipped items live in EquipmentManager, not here.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    public class Inventory : MonoBehaviour
    {
        [Tooltip("Items the player owns at boot. Stand-in for the future loot system.")]
        [SerializeField] private List<EquipmentData> startingItems = new List<EquipmentData>();

        private readonly List<ItemInstance> items = new List<ItemInstance>();

        public IReadOnlyList<ItemInstance> Items => items;

        // The equipment screen refreshes on this.
        public event Action OnInventoryChanged;

        private void Start()
        {
            foreach (EquipmentData data in startingItems)
            {
                if (data != null)
                {
                    items.Add(new ItemInstance(data));
                }
            }

            OnInventoryChanged?.Invoke();
        }

        public void Add(ItemInstance item)
        {
            if (item == null)
            {
                return;
            }

            items.Add(item);
            OnInventoryChanged?.Invoke();
        }

        public bool Remove(ItemInstance item)
        {
            bool removed = items.Remove(item);
            if (removed)
            {
                OnInventoryChanged?.Invoke();
            }

            return removed;
        }
    }
}
