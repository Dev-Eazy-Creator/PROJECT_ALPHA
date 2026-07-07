// Applies equipped augments' stat modifiers to CharacterStats. Behavioral augments are data-only in Part 2.
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    // Runs after CharacterStats (-90) builds its set, and before the pools (0) init so they see the bonuses.
    [DefaultExecutionOrder(-80)]
    public class AugmentEquipper : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private List<AugmentData> equippedAugments = new List<AugmentData>();

        private void Start()
        {
            ApplyAll();
        }

        // Re-apply from scratch (public so a future equip UI / debug can call it when the list changes).
        public void ApplyAll()
        {
            if (stats == null || stats.Attributes == null)
            {
                Debug.LogWarning("[AugmentEquipper] No CharacterStats/Attributes; cannot apply augments.", this);
                return;
            }

            for (int i = 0; i < equippedAugments.Count; i++)
            {
                AugmentData augment = equippedAugments[i];
                if (augment == null)
                {
                    continue;
                }

                // Clear any prior application of this augment so ApplyAll is idempotent.
                stats.Attributes.RemoveAllModifiersFromSource(augment);

                if (augment.EffectKind == AugmentEffectKind.StatModifier)
                {
                    // The AugmentData asset is the modifier Source, so it detaches cleanly (like gear will).
                    StatModifier modifier = new StatModifier(
                        augment.TargetAttribute, augment.ModifierType, augment.ModifierValue, augment);
                    stats.Attributes.AddModifier(modifier);
                }
                else
                {
                    // Behavioral augments are data-only in Part 2 — no consumer reads them yet.
                    Debug.Log($"[AugmentEquipper] Skipping behavioral augment '{augment.DisplayName}' " +
                              "(data-only in Part 2).", this);
                }
            }
        }
    }
}
