// Base authoring asset for any item. Not created directly — see EquipmentData/WeaponData/ArmorData.
using UnityEngine;

namespace ProjectAlpha
{
    public class ItemData : ScriptableObject
    {
        public string DisplayName;
        public Sprite Icon;                       // for the future inventory UI
        [TextArea] public string Description;
        [Tooltip("Drop-chance weight for the future loot system. Data-only in Part 3; nothing reads it.")]
        public float DropWeight = 1f;
    }
}
