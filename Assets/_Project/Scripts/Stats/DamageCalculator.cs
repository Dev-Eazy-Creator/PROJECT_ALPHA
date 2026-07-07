// Single source of truth for converting attacker/defender stats into a final damage number.
// Swap this formula freely during balancing — nothing else should compute damage.
using UnityEngine;

namespace ProjectAlpha
{
    public static class DamageCalculator
    {
        // Part 2 formula (conventional, easy to reason about, easy to replace):
        // final = attackPower * (attackPower / (attackPower + defense)), min 1.
        // This gives diminishing returns from defense without ever fully negating damage.
        public static float ComputeDamage(float attackPower, float defense)
        {
            float raw = attackPower * (attackPower / Mathf.Max(1f, attackPower + defense));
            return Mathf.Max(1f, raw);
        }
    }
}
