// Armor: equipment with no extra fields. A distinct type + Create menu entry for clarity.
using UnityEngine;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "New_Armor", menuName = "Game/Gear/Armor")]
    public class ArmorData : EquipmentData
    {
    }
}
