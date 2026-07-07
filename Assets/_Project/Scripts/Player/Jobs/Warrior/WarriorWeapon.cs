// Warrior job weapon script. Implements IWeapon. Combat logic added in Part 2.
using UnityEngine;

namespace ProjectAlpha
{
    public class WarriorWeapon : MonoBehaviour, IWeapon
    {
        public JobType WeaponJobType => JobType.Warrior;

        public void OnEquip() { /* Part 2 */ }
        public void OnUnequip() { /* Part 2 */ }
    }
}
