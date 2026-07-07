// A bounded, depleting/regenerating current value tied to a max-value attribute.
// Base class for Stamina now, Health later.
using System;
using UnityEngine;

namespace ProjectAlpha
{
    public class ResourcePool
    {
        public float Current { get; private set; }
        public float Max { get; private set; }

        // (current, max) — fires on any change, for the debug readout now and a real HUD later.
        public event Action<float, float> OnChanged;

        public bool IsEmpty => Current <= 0f;
        public float Normalized => Max <= 0f ? 0f : Current / Max;

        public void Initialize(float max, bool fillToMax = true)
        {
            Max = Mathf.Max(0f, max);
            Current = fillToMax ? Max : Mathf.Clamp(Current, 0f, Max);
            OnChanged?.Invoke(Current, Max);
        }

        // Updates Max and clamps Current (does not refill) — used when the max changes mid-play.
        public void SetMax(float newMax)
        {
            Max = Mathf.Max(0f, newMax);
            Current = Mathf.Clamp(Current, 0f, Max);
            OnChanged?.Invoke(Current, Max);
        }

        // Instantaneous cost: spends only if affordable. Returns whether it spent.
        public bool Spend(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }
            if (Current < amount)
            {
                return false;
            }
            Current -= amount;
            OnChanged?.Invoke(Current, Max);
            return true;
        }

        // Continuous per-frame drain (sprint/climb). Clamps to 0.
        public void Drain(float amountThisFrame)
        {
            if (amountThisFrame <= 0f)
            {
                return;
            }
            Current = Mathf.Max(0f, Current - amountThisFrame);
            OnChanged?.Invoke(Current, Max);
        }

        // Continuous per-frame regen. Clamps to Max.
        public void Regenerate(float amountThisFrame)
        {
            if (amountThisFrame <= 0f)
            {
                return;
            }
            Current = Mathf.Min(Max, Current + amountThisFrame);
            OnChanged?.Invoke(Current, Max);
        }
    }
}
