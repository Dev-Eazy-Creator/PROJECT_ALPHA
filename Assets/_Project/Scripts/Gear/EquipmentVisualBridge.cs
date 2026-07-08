// Bridges our gear system to the N_Hance NHAvatar so ARMOR meshes appear on the character (and in the
// inventory preview) as EquipmentManager equips/unequips. Weapons stay on EquipmentManager's own spawn;
// rings/amulets have no mesh. This is the ONLY file that references N_Hance types, keeping that
// third-party dependency isolated behind our OnEquipmentChanged event.
using System.Collections.Generic;
using UnityEngine;
using NHance.Assets.Scripts;
using NHance.Assets.Scripts.Enums;
using NHance.Assets.Scripts.Items;

namespace ProjectAlpha
{
    // Runs after NHAvatar and EquipmentManager have initialised/equipped, so the initial sync sees a
    // ready avatar and the fully-populated Equipped set.
    [DefaultExecutionOrder(50)]
    public class EquipmentVisualBridge : MonoBehaviour
    {
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private NHAvatar avatar;
        [Tooltip("Optional: the character's hair GameObject (a static mesh, not a N_Hance item). Hidden " +
                 "while a mesh item occupies the Head slot — N_Hance only auto-hides hair that is itself an NHItem.")]
        [SerializeField] private GameObject hairToHideUnderHelmet;

        // The N_Hance item type(s) currently shown per slot, so unequip can clear them precisely (by
        // which point the ItemInstance reference is already gone). A single gear piece can be several
        // meshes (e.g. leg armor = pants + greaves + belt), so each slot maps to a LIST of types.
        private readonly Dictionary<EquipmentSlot, List<ItemTypeEnum>> shown = new Dictionary<EquipmentSlot, List<ItemTypeEnum>>();
        private bool synced;

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged += HandleEquipmentChanged;
            }
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= HandleEquipmentChanged;
            }
        }

        private void Start()
        {
            // Apply everything already equipped (starting gear) once, with the avatar fully ready.
            SyncAll();
            synced = true;
        }

        private void HandleEquipmentChanged(EquipmentSlot slot, ItemInstance item)
        {
            // Startup equips are handled by SyncAll(); only drive incremental changes after that.
            if (!synced || avatar == null || !IsArmorSlot(slot))
            {
                return;
            }

            Apply(slot, item);
            Rebuild();
            RefreshHair();
        }

        private void SyncAll()
        {
            if (equipment == null || avatar == null)
            {
                return;
            }

            bool changed = false;
            foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipment.Equipped)
            {
                if (IsArmorSlot(pair.Key))
                {
                    Apply(pair.Key, pair.Value);
                    changed = true;
                }
            }
            if (changed)
            {
                Rebuild();
            }
            RefreshHair();
        }

        // N_Hance's Compile() only ever HIDES body parts and always re-instantiates every item in Items,
        // so calling it alone would duplicate already-shown pieces and never re-show a part after unequip.
        // Clean() first destroys all item instances and re-shows every body part; Compile() then rebuilds
        // the current set and re-hides only what's still equipped. Correct + duplicate-free every change.
        private void Rebuild()
        {
            avatar.Clean();
            avatar.Compile();
        }

        // N_Hance only auto-hides hair that is itself an NHItem; static hair meshes are ours to toggle.
        // Derived from the current Head slot and run AFTER Rebuild(), since Clean() re-shows body parts.
        private void RefreshHair()
        {
            if (hairToHideUnderHelmet == null)
            {
                return;
            }

            ItemInstance head = null;
            if (equipment != null)
            {
                equipment.Equipped.TryGetValue(EquipmentSlot.Head, out head);
            }

            bool headHasMesh = head != null && head.Data != null
                && head.Data.VisualPrefabs != null && head.Data.VisualPrefabs.Count > 0;
            hairToHideUnderHelmet.SetActive(!headHasMesh);
        }

        // Stages the visuals for one slot (clears the previous meshes, sets the new ones). A piece may be
        // several N_Hance items. Callers call Compile() after.
        private void Apply(EquipmentSlot slot, ItemInstance item)
        {
            if (shown.TryGetValue(slot, out List<ItemTypeEnum> previous))
            {
                for (int i = 0; i < previous.Count; i++)
                {
                    avatar.ClearItems(previous[i]);
                }
                shown.Remove(slot);
            }

            List<ItemTypeEnum> applied = null;
            List<GameObject> prefabs = item != null && item.Data != null ? item.Data.VisualPrefabs : null;
            if (prefabs != null)
            {
                foreach (GameObject prefab in prefabs)
                {
                    if (prefab == null)
                    {
                        continue;
                    }
                    NHItem visual = prefab.GetComponent<NHItem>();
                    if (visual == null)
                    {
                        continue;
                    }

                    avatar.SetItem(visual);
                    (applied ??= new List<ItemTypeEnum>()).Add(visual.Type);
                }
            }

            if (applied != null)
            {
                shown[slot] = applied;
            }
        }

        private static bool IsArmorSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Head:
                case EquipmentSlot.Chest:
                case EquipmentSlot.Gloves:
                case EquipmentSlot.Legs:
                case EquipmentSlot.Boots:
                case EquipmentSlot.Cape:
                    return true;
                default:
                    return false;
            }
        }
    }
}
