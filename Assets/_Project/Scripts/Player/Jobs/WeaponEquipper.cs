// Instantiates and attaches the correct weapon prefab when the job changes.
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectAlpha
{
    public class WeaponEquipper : MonoBehaviour
    {
        [FormerlySerializedAs("vocationManager")]
        [SerializeField] private JobManager jobManager;
        [SerializeField] private Transform weaponAnchor;

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
            // Set up the starting weapon from the current job.
            if (jobManager != null)
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
