// Legacy Part 1 weapon spawner. Superseded by EquipmentManager (Part 3), which spawns the
// equipped weapon's prefab. Disabled by default; enable spawnWeaponOnJobChange only if using the
// old job-driven weapon without the gear system.
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectAlpha
{
    public class WeaponEquipper : MonoBehaviour
    {
        [FormerlySerializedAs("vocationManager")]
        [SerializeField] private JobManager jobManager;
        [SerializeField] private Transform weaponAnchor;
        [Tooltip("Part 3 EquipmentManager owns weapon spawning. Leave OFF unless not using gear.")]
        [SerializeField] private bool spawnWeaponOnJobChange = false;

        private void Awake()
        {
            if (spawnWeaponOnJobChange && jobManager != null)
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
            // Set up the starting weapon from the current job (only when this legacy spawner is enabled).
            if (spawnWeaponOnJobChange && jobManager != null)
            {
                Equip(jobManager.CurrentJob);
            }
        }

        private void HandleJobChanged(JobData newJob)
        {
            Equip(newJob);
        }

        private void Equip(JobData job)
        {
            if (weaponAnchor == null)
            {
                return;
            }

            // Remove any weapon currently attached.
            for (int i = weaponAnchor.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponAnchor.GetChild(i).gameObject);
            }

            if (job == null || job.WeaponPrefab == null)
            {
                // Null weapon prefab is handled gracefully — nothing to equip.
                return;
            }

            GameObject weapon = Instantiate(job.WeaponPrefab, weaponAnchor);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            if (weapon.TryGetComponent(out IWeapon weaponComponent))
            {
                weaponComponent.OnEquip();
            }
        }
    }
}
