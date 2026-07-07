// A concrete owned/equipped item: its EquipmentData asset plus its per-instance upgrade level.
// Plain C# (not a MonoBehaviour/SO). This is the modifier Source, so removal targets THIS item.
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    public class ItemInstance
    {
        public EquipmentData Data { get; }
        public int UpgradeLevel { get; private set; }

        public ItemInstance(EquipmentData data, int upgradeLevel = 0)
        {
            Data = data;
            UpgradeLevel = Clamp(upgradeLevel);
        }

        public void SetUpgradeLevel(int level)
        {
            UpgradeLevel = Clamp(level);
        }

        // The StatModifiers this instance contributes at its current level, sourced to itself.
        public IEnumerable<StatModifier> BuildModifiers()
        {
            if (Data == null || Data.Modifiers == null)
            {
                yield break;
            }

            foreach (GearModifier mod in Data.Modifiers)
            {
                float value = mod.BaseValue + UpgradeLevel * mod.PerLevelValue;
                yield return new StatModifier(mod.Attribute, mod.Type, value, this);
            }
        }

        private int Clamp(int level)
        {
            int max = Data != null ? Data.MaxUpgradeLevel : 0;
            return Mathf.Clamp(level, 0, max);
        }
    }
}
