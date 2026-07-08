// Anything equippable into a slot (armor + accessories). Carries stat modifiers, job-lock, upgrade cap.
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "New_Equipment", menuName = "Game/Gear/Equipment")]
    public class EquipmentData : ItemData
    {
        public EquipmentSlot Slot;
        [Tooltip("Jobs allowed to equip this. EMPTY = any job.")]
        public List<JobType> AllowedJobs = new List<JobType>();
        public List<GearModifier> Modifiers = new List<GearModifier>();
        [Min(0)] public int MaxUpgradeLevel = 3;   // DD1: three enhancement stars by default
        [Tooltip("Optional N_Hance armor prefabs (each with an NHItem component) shown on the character " +
                 "when equipped. One piece may be several meshes, e.g. leg armor = pants + greaves + belt. " +
                 "Armor slots only; weapons keep the EquipmentManager spawn, rings/amulets have no mesh.")]
        public List<GameObject> VisualPrefabs = new List<GameObject>();
    }
}
