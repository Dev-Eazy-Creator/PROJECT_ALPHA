// Warrior vocation weapon script. Implements IWeapon. Combat logic added in Part 2.
using UnityEngine;

namespace ProjectAlpha
{
    public class WarriorWeapon : MonoBehaviour, IWeapon
    {
        public VocationType WeaponVocationType => VocationType.Warrior;

        public void OnEquip() { /* Part 2 */ }
        public void OnUnequip() { /* Part 2 */ }
    }
}
