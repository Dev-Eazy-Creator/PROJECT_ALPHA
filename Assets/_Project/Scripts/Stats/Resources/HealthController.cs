// The health pool. Bounded by the MaxHealth attribute. Exposes TakeDamage / Heal.
// The same entry point combat will call later; the Part 2 debug input is a temporary caller.
using System;
using UnityEngine;

namespace ProjectAlpha
{
    public class HealthController : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;

        private readonly ResourcePool health = new ResourcePool();
        public ResourcePool Health => health;

        // Fires once when health first reaches zero. A listener decides what happens (no destroy here).
        public event Action OnDied;

        private Attribute maxHealthAttribute;
        private bool hasDied;

        private void Start()
        {
            if (stats == null)
            {
                Debug.LogError("[HealthController] No CharacterStats assigned.", this);
                enabled = false;
                return;
            }

            health.Initialize(stats.GetValue(AttributeType.MaxHealth));

            stats.OnStatsRebuilt += HandleStatsRebuilt;
            maxHealthAttribute = stats.Attributes != null ? stats.Attributes.Get(AttributeType.MaxHealth) : null;
            if (maxHealthAttribute != null)
            {
                maxHealthAttribute.OnValueChanged += HandleMaxHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                stats.OnStatsRebuilt -= HandleStatsRebuilt;
            }
            if (maxHealthAttribute != null)
            {
                maxHealthAttribute.OnValueChanged -= HandleMaxHealthChanged;
            }
        }

        private void HandleStatsRebuilt() => health.SetMax(stats.GetValue(AttributeType.MaxHealth));
        private void HandleMaxHealthChanged() => health.SetMax(stats.GetValue(AttributeType.MaxHealth));

        public void TakeDamage(float amount)
        {
            if (amount < 0f)
            {
                amount = 0f;
            }

            health.Drain(amount);

            if (health.IsEmpty && !hasDied)
            {
                hasDied = true;
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            health.Regenerate(amount);
            if (!health.IsEmpty)
            {
                hasDied = false;
            }
        }
    }
}
