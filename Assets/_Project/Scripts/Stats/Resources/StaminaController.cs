// The stamina pool plus DD1 drain/regen rules. Drains on sprint and climb, regenerates
// otherwise after a short delay. Gates sprinting via PlayerMovement.SprintAllowed.
using UnityEngine;

namespace ProjectAlpha
{
    public class StaminaController : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private PlayerMovement movement;   // reads IsSprinting, writes SprintAllowed
        [SerializeField] private PlayerClimb climb;         // reads IsClimbing
        [Tooltip("Seconds after the last drain before stamina begins regenerating.")]
        [SerializeField] private float regenDelay = 1.0f;
        [Tooltip("Stamina must climb above this before sprint is re-permitted after hitting zero.")]
        [SerializeField] private float sprintRecoverThreshold = 15f;

        private readonly ResourcePool stamina = new ResourcePool();
        public ResourcePool Stamina => stamina;

        private Attribute maxStaminaAttribute;
        private float lastDrainTime;
        private bool isSprintLocked;

        private void Start()
        {
            if (stats == null)
            {
                Debug.LogError("[StaminaController] No CharacterStats assigned.", this);
                enabled = false;
                return;
            }

            // Init full to the current MaxStamina (augments already applied this frame via execution order).
            stamina.Initialize(stats.GetValue(AttributeType.MaxStamina));
            lastDrainTime = -regenDelay;

            stats.OnStatsRebuilt += HandleStatsRebuilt;
            maxStaminaAttribute = stats.Attributes != null ? stats.Attributes.Get(AttributeType.MaxStamina) : null;
            if (maxStaminaAttribute != null)
            {
                maxStaminaAttribute.OnValueChanged += HandleMaxStaminaChanged;
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                stats.OnStatsRebuilt -= HandleStatsRebuilt;
            }
            if (maxStaminaAttribute != null)
            {
                maxStaminaAttribute.OnValueChanged -= HandleMaxStaminaChanged;
            }
        }

        // Job change: resize (clamp, do not refill) so a job swap doesn't hand the player free stamina.
        private void HandleStatsRebuilt() => stamina.SetMax(stats.GetValue(AttributeType.MaxStamina));

        // Live modifier (augment/gear/debug) changed MaxStamina: resize the pool without refilling.
        private void HandleMaxStaminaChanged() => stamina.SetMax(stats.GetValue(AttributeType.MaxStamina));

        private void Update()
        {
            if (stats == null)
            {
                return;
            }

            // Determine drain this frame (climb takes priority over sprint).
            bool draining = false;
            float ratePerSecond = 0f;

            if (climb != null && climb.IsClimbing)
            {
                ratePerSecond = stats.GetValue(AttributeType.StaminaDrainClimb);
                draining = true;
            }
            else if (movement != null && movement.IsSprinting)
            {
                ratePerSecond = stats.GetValue(AttributeType.StaminaDrainSprint);
                draining = true;
            }

            if (draining)
            {
                stamina.Drain(ratePerSecond * Time.deltaTime);
                lastDrainTime = Time.time;
            }
            else if (Time.time - lastDrainTime >= regenDelay)
            {
                stamina.Regenerate(stats.GetValue(AttributeType.StaminaRegenRate) * Time.deltaTime);
            }

            // Sprint gating: forbid while empty, and keep forbidden until recovered past the threshold.
            if (stamina.Current <= 0f)
            {
                isSprintLocked = true;
            }
            else if (isSprintLocked && stamina.Current >= sprintRecoverThreshold)
            {
                isSprintLocked = false;
            }

            if (movement != null)
            {
                movement.SprintAllowed = !isSprintLocked;
            }
        }
    }
}
