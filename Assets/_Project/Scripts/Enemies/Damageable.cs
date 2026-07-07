// Marks a GameObject as able to receive damage. Combat hitboxes (Part 3+) call this.
using UnityEngine;

namespace ProjectAlpha
{
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private HealthController health;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<HealthController>();
            }
        }

        public void ApplyDamage(float amount)
        {
            if (health != null)
            {
                health.TakeDamage(amount);
            }
        }
    }
}
