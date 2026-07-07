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
    }
}
