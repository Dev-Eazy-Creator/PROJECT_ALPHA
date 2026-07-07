// A weapon: equipment plus weapon class, damage type, stagger/knockdown power, and a spawn prefab.
using UnityEngine;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "New_Weapon", menuName = "Game/Gear/Weapon")]
    public class WeaponData : EquipmentData
    {
        public WeaponClass Class;
        public DamageType DamageType;              // Physical / Magick (elemental added later)
        public float StaggerPower;                 // consumed by combat later; authored now
        public float KnockdownPower;
        public GameObject WeaponPrefab;            // spawned at the Part 1 WeaponAnchor
    }
}
