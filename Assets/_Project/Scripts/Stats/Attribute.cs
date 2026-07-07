// One attribute: a base value plus a list of modifiers, with a cached, recomputed final value.
using System;
using System.Collections.Generic;

namespace ProjectAlpha
{
    public class Attribute
    {
        public AttributeType Type { get; }
        public float BaseValue { get; private set; }

        private readonly List<StatModifier> modifiers = new List<StatModifier>();
        private float cachedValue;
        private bool isDirty = true;

        // Fires whenever the final value may have changed, so resource pools bound to this attribute rescale.
        public event Action OnValueChanged;

        public Attribute(AttributeType type, float baseValue)
        {
            Type = type;
            BaseValue = baseValue;
        }

        public float Value
        {
            get
            {
                if (isDirty)
                {
                    cachedValue = Recompute();
                    isDirty = false;
                }
                return cachedValue;
            }
        }

        public void SetBaseValue(float value)
        {
            if (BaseValue == value)
            {
                return;
            }
            BaseValue = value;
            MarkDirtyAndNotify();
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }
            modifiers.Add(modifier);
            MarkDirtyAndNotify();
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            if (modifiers.Remove(modifier))
            {
                MarkDirtyAndNotify();
                return true;
            }
            return false;
        }

        // Removes every modifier whose Source matches; returns how many were removed.
        public int RemoveAllModifiersFromSource(object source)
        {
            int removed = modifiers.RemoveAll(m => m.Source == source);
            if (removed > 0)
            {
                MarkDirtyAndNotify();
            }
            return removed;
        }

        private void MarkDirtyAndNotify()
        {
            isDirty = true;
            OnValueChanged?.Invoke();
        }

        // Non-destructive "what if" value: recomputes as though every modifier from removeSource were gone
        // and addModifiers (those targeting this attribute) were present. Used by the equip-comparison UI;
        // does not mutate the live modifier set. Exact across Flat/PercentAdd/PercentMult.
        public float PreviewValue(object removeSource, IEnumerable<StatModifier> addModifiers)
        {
            List<StatModifier> hypothetical = new List<StatModifier>(modifiers.Count + 4);
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Source != removeSource)
                {
                    hypothetical.Add(modifiers[i]);
                }
            }
            if (addModifiers != null)
            {
                foreach (StatModifier mod in addModifiers)
                {
                    if (mod != null && mod.Attribute == Type)
                    {
                        hypothetical.Add(mod);
                    }
                }
            }
            return Compute(hypothetical);
        }

        private float Recompute() => Compute(modifiers);

        // Fixed pipeline: base -> Flat (sum) -> PercentAdd (sum, apply once) -> PercentMult (sequential).
        private float Compute(List<StatModifier> mods)
        {
            float result = BaseValue;
            float sumPercentAdd = 0f;

            for (int i = 0; i < mods.Count; i++)
            {
                StatModifier mod = mods[i];
                switch (mod.Type)
                {
                    case StatModifierType.Flat:
                        result += mod.Value;
                        break;
                    case StatModifierType.PercentAdd:
                        sumPercentAdd += mod.Value;
                        break;
                }
            }

            result *= 1f + sumPercentAdd;

            for (int i = 0; i < mods.Count; i++)
            {
                StatModifier mod = mods[i];
                if (mod.Type == StatModifierType.PercentMult)
                {
                    result *= 1f + mod.Value;
                }
            }

            return result;
        }
    }
}
