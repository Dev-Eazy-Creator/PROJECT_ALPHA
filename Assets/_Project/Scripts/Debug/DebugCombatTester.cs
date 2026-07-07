// Test-only: press P to deal real PhysicalAttack-vs-PhysicalDefense damage to the dummy. Throwaway.
// When real combat lands, a hitbox calls Damageable.ApplyDamage with DamageCalculator output instead.
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAlpha
{
    public class DebugCombatTester : MonoBehaviour
    {
        [SerializeField] private CharacterStats playerStats;
        [SerializeField] private Damageable target;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame)
            {
                return;
            }

            if (playerStats == null || target == null)
            {
                Debug.LogWarning("[DebugCombat] playerStats or target not assigned.", this);
                return;
            }

            CharacterStats enemyStats = target.GetComponent<CharacterStats>();
            float attack = playerStats.GetValue(AttributeType.PhysicalAttack);
            float defense = enemyStats != null ? enemyStats.GetValue(AttributeType.PhysicalDefense) : 0f;

            float damage = DamageCalculator.ComputeDamage(attack, defense);
            target.ApplyDamage(damage);

            HealthController enemyHealth = target.GetComponent<HealthController>();
            float remaining = enemyHealth != null ? enemyHealth.Health.Current : -1f;
            Debug.Log($"[DebugCombat] PhysicalAttack={attack:0.##}  PhysicalDefense={defense:0.##}  " +
                      $"damage={damage:0.##}  remainingHealth={remaining:0.##}", this);
        }
    }
}
