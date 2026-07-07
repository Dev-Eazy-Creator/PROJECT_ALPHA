// Test-only: press 1..9 to equip a test item, U to unequip, = to upgrade — watch stats change. Throwaway.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAlpha
{
    public class DebugEquipmentTester : MonoBehaviour
    {
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private CharacterStats stats;
        [Tooltip("Press number keys 1..9 to equip the matching item.")]
        [SerializeField] private List<EquipmentData> testItems = new List<EquipmentData>();

        private EquipmentSlot lastSlot;
        private bool hasLastSlot;

        private void Update()
        {
            if (Keyboard.current == null || equipment == null)
            {
                return;
            }

            for (int i = 0; i < testItems.Count && i < 9; i++)
            {
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    EquipmentData data = testItems[i];
                    if (data == null)
                    {
                        continue;
                    }
                    if (equipment.Equip(new ItemInstance(data)))
                    {
                        lastSlot = data.Slot;
                        hasLastSlot = true;
                        LogState($"equipped {data.DisplayName}");
                    }
                }
            }

            if (hasLastSlot && Keyboard.current.uKey.wasPressedThisFrame)
            {
                equipment.Unequip(lastSlot);
                LogState($"unequipped {lastSlot}");
            }

            if (hasLastSlot && Keyboard.current.equalsKey.wasPressedThisFrame)
            {
                equipment.UpgradeEquipped(lastSlot);
                LogState($"upgraded {lastSlot}");
            }
        }

        private void LogState(string action)
        {
            if (stats == null)
            {
                Debug.Log($"[DebugEquip] {action}", this);
                return;
            }
            Debug.Log($"[DebugEquip] {action}  |  " +
                      $"HP {stats.GetValue(AttributeType.MaxHealth):0}  " +
                      $"PAtk {stats.GetValue(AttributeType.PhysicalAttack):0.#}  " +
                      $"PDef {stats.GetValue(AttributeType.PhysicalDefense):0.#}  " +
                      $"Stam {stats.GetValue(AttributeType.MaxStamina):0}  " +
                      $"MAtk {stats.GetValue(AttributeType.MagickAttack):0.#}", this);
        }
    }
}
