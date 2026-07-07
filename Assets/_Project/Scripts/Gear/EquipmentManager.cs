// Holds equipped items per slot, applies/removes their stat modifiers, enforces job-locks,
// and spawns weapon visuals. The single entry point for equip/unequip/upgrade.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    // Applies gear modifiers before the resource pools initialize (like AugmentEquipper), so
    // starting gear is included in the initial Health/Stamina maxima.
    [DefaultExecutionOrder(-80)]
    public class EquipmentManager : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private JobManager jobManager;
        [SerializeField] private Transform weaponAnchor;
        [Tooltip("Gear equipped at boot (e.g. the starting weapon + armor).")]
        [SerializeField] private List<EquipmentData> startingEquipment = new List<EquipmentData>();

        private readonly Dictionary<EquipmentSlot, ItemInstance> equipped =
            new Dictionary<EquipmentSlot, ItemInstance>();
        private readonly Dictionary<EquipmentSlot, GameObject> spawnedWeapons =
            new Dictionary<EquipmentSlot, GameObject>();

        public IReadOnlyDictionary<EquipmentSlot, ItemInstance> Equipped => equipped;
        public event Action<EquipmentSlot, ItemInstance> OnEquipmentChanged;

        private void Awake()
        {
            if (jobManager != null)
            {
                jobManager.OnJobChanged += HandleJobChanged;
            }
        }

        private void OnDestroy()
        {
            if (jobManager != null)
            {
                jobManager.OnJobChanged -= HandleJobChanged;
            }
        }

        private void Start()
        {
            for (int i = 0; i < startingEquipment.Count; i++)
            {
                if (startingEquipment[i] != null)
                {
                    Equip(new ItemInstance(startingEquipment[i]));
                }
            }
        }

        public bool CanEquip(ItemInstance item, out string reason)
        {
            reason = null;
            if (item == null || item.Data == null)
            {
                reason = "no item";
                return false;
            }

            List<JobType> allowed = item.Data.AllowedJobs;
            if (allowed != null && allowed.Count > 0)
            {
                JobData current = jobManager != null ? jobManager.CurrentJob : null;
                if (current == null || !allowed.Contains(current.Job))
                {
                    reason = $"job-locked (needs {string.Join("/", allowed)})";
                    return false;
                }
            }
            return true;
        }

        public bool Equip(ItemInstance item)
        {
            if (!CanEquip(item, out string reason))
            {
                Debug.Log($"[EquipmentManager] Cannot equip '{DisplayName(item)}': {reason}.", this);
                return false;
            }

            EquipmentSlot slot = item.Data.Slot;
            Unequip(slot);   // clear the current occupant first

            equipped[slot] = item;
            ApplyModifiers(item);

            if (item.Data is WeaponData weapon && IsWeaponSlot(slot))
            {
                SpawnWeapon(slot, weapon);
            }

            OnEquipmentChanged?.Invoke(slot, item);
            return true;
        }

        public void Unequip(EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(slot, out ItemInstance item) || item == null)
            {
                return;
            }

            if (stats != null && stats.Attributes != null)
            {
                stats.Attributes.RemoveAllModifiersFromSource(item);
            }
            DespawnWeapon(slot);
            equipped.Remove(slot);
            OnEquipmentChanged?.Invoke(slot, null);
        }

        // Bumps the equipped item's upgrade level and re-applies its (now higher) modifiers live.
        public void UpgradeEquipped(EquipmentSlot slot)
        {
            if (!equipped.TryGetValue(slot, out ItemInstance item) || item == null)
            {
                return;
            }
            if (item.UpgradeLevel >= item.Data.MaxUpgradeLevel)
            {
                return;
            }

            if (stats != null && stats.Attributes != null)
            {
                stats.Attributes.RemoveAllModifiersFromSource(item);
            }
            item.SetUpgradeLevel(item.UpgradeLevel + 1);
            ApplyModifiers(item);
            OnEquipmentChanged?.Invoke(slot, item);
        }

        private void ApplyModifiers(ItemInstance item)
        {
            if (stats == null || stats.Attributes == null)
            {
                return;
            }
            foreach (StatModifier mod in item.BuildModifiers())
            {
                stats.Attributes.AddModifier(mod);
            }
        }

        // On a job change, auto-unequip anything the new job no longer allows.
        private void HandleJobChanged(JobData newJob)
        {
            List<EquipmentSlot> toRemove = null;
            foreach (KeyValuePair<EquipmentSlot, ItemInstance> pair in equipped)
            {
                if (!CanEquip(pair.Value, out string reason))
                {
                    Debug.Log($"[EquipmentManager] Job change unequips '{DisplayName(pair.Value)}': {reason}.", this);
                    (toRemove ??= new List<EquipmentSlot>()).Add(pair.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (EquipmentSlot slot in toRemove)
                {
                    Unequip(slot);
                }
            }
        }

        private static bool IsWeaponSlot(EquipmentSlot slot) =>
            slot == EquipmentSlot.PrimaryWeapon || slot == EquipmentSlot.SecondaryWeapon;

        private void SpawnWeapon(EquipmentSlot slot, WeaponData weapon)
        {
            DespawnWeapon(slot);
            if (weapon.WeaponPrefab == null || weaponAnchor == null)
            {
                return;
            }

            GameObject go = Instantiate(weapon.WeaponPrefab, weaponAnchor);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            spawnedWeapons[slot] = go;

            if (go.TryGetComponent(out IWeapon weaponComponent))
            {
                weaponComponent.OnEquip();
            }
        }

        private void DespawnWeapon(EquipmentSlot slot)
        {
            if (spawnedWeapons.TryGetValue(slot, out GameObject go) && go != null)
            {
                if (go.TryGetComponent(out IWeapon weaponComponent))
                {
                    weaponComponent.OnUnequip();
                }
                Destroy(go);
            }
            spawnedWeapons.Remove(slot);
        }

        private static string DisplayName(ItemInstance item) =>
            item != null && item.Data != null ? item.Data.DisplayName : "null";
    }
}
