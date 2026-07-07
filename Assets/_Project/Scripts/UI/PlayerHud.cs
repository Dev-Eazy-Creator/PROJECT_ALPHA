// Root player HUD: binds the player's health and stamina pools to their on-screen bars.
using UnityEngine;

namespace ProjectAlpha
{
    public class PlayerHud : MonoBehaviour
    {
        [SerializeField] private HealthController health;
        [SerializeField] private StaminaController stamina;
        [SerializeField] private ResourceBarView healthBar;
        [SerializeField] private ResourceBarView staminaBar;

        private void Start()
        {
            // Pools exist from construction; Bind refreshes now and subscribes for future changes,
            // so this is correct regardless of whether the controllers have initialized yet.
            if (health != null && healthBar != null)
            {
                healthBar.Bind(health.Health);
            }
            if (stamina != null && staminaBar != null)
            {
                staminaBar.Bind(stamina.Stamina);
            }
        }
    }
}
