// Test helper on the enemy dummy: on death, logs and refills to full so it can be re-damaged.
using UnityEngine;

namespace ProjectAlpha
{
    [RequireComponent(typeof(HealthController))]
    public class DummyHealthReset : MonoBehaviour
    {
        [SerializeField] private HealthController health;
        [SerializeField] private CharacterStats stats;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<HealthController>();
            }
            if (stats == null)
            {
                stats = GetComponent<CharacterStats>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDied -= HandleDied;
            }
        }

        private void HandleDied()
        {
            float max = stats != null ? stats.GetValue(AttributeType.MaxHealth) : 0f;
            Debug.Log($"[Dummy] Health reached 0 — refilling to {max:0} for re-testing.", this);
            health.Heal(max);
        }
    }
}
