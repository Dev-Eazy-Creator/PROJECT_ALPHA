// Instantiates and attaches the correct weapon prefab when the vocation changes.
using UnityEngine;

namespace ProjectAlpha
{
    public class WeaponEquipper : MonoBehaviour
    {
        [SerializeField] private VocationManager vocationManager;
        [SerializeField] private Transform weaponAnchor;

        private void Awake()
        {
            if (vocationManager != null)
            {
                vocationManager.OnVocationChanged += HandleVocationChanged;
            }
        }

        private void OnDestroy()
        {
            if (vocationManager != null)
            {
                vocationManager.OnVocationChanged -= HandleVocationChanged;
            }
        }

        private void Start()
        {
            // Set up the starting weapon from the current vocation.
            if (vocationManager != null)
            {
                Equip(vocationManager.CurrentVocation);
            }
        }

        private void HandleVocationChanged(VocationData newVocation)
        {
            Equip(newVocation);
        }

        private void Equip(VocationData vocation)
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

            if (vocation == null || vocation.WeaponPrefab == null)
            {
                // Null weapon prefab is handled gracefully — nothing to equip.
                return;
            }

            GameObject weapon = Instantiate(vocation.WeaponPrefab, weaponAnchor);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;

            if (weapon.TryGetComponent(out IWeapon weaponComponent))
            {
                weaponComponent.OnEquip();
            }
        }
    }
}
