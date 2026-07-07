// Drives Animator parameters based on player state. Single source of truth for animation state.
using UnityEngine;

namespace ProjectAlpha
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerJump jump;
        [SerializeField] private PlayerClimb climb;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private VocationManager vocationManager;

        private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsClimbingHash = Animator.StringToHash("isClimbing");
        private static readonly int VocationIndexHash = Animator.StringToHash("vocationIndex");
        // Extra param so the Jump_Rise / Jump_Fall states can branch on ascent vs. descent.
        private static readonly int VerticalVelocityHash = Animator.StringToHash("verticalVelocity");
        // Splits locomotion into Walk vs. Run.
        private static readonly int IsSprintingHash = Animator.StringToHash("isSprinting");

        private void Awake()
        {
            // Robust against swapping the placeholder capsule for a real character model:
            // if the reference wasn't re-assigned, grab the Animator from the model child.
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (movement == null)
            {
                movement = GetComponent<PlayerMovement>();
            }
        }

        private void OnEnable()
        {
            if (vocationManager != null)
            {
                vocationManager.OnVocationChanged += HandleVocationChanged;
            }
        }

        private void OnDisable()
        {
            if (vocationManager != null)
            {
                vocationManager.OnVocationChanged -= HandleVocationChanged;
            }
        }

        private void Start()
        {
            // Apply the starting vocation's override controller and index.
            if (vocationManager != null && vocationManager.CurrentVocation != null)
            {
                HandleVocationChanged(vocationManager.CurrentVocation);
            }
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(MoveSpeedHash, motor != null ? motor.HorizontalSpeed : 0f);
            animator.SetBool(IsGroundedHash, jump != null && jump.IsGrounded);
            animator.SetBool(IsClimbingHash, climb != null && climb.IsClimbing);
            animator.SetFloat(VerticalVelocityHash, jump != null ? jump.VerticalVelocity : 0f);
            animator.SetBool(IsSprintingHash, movement != null && movement.IsSprinting);
        }

        private void HandleVocationChanged(VocationData newVocation)
        {
            if (animator == null || newVocation == null)
            {
                return;
            }

            animator.SetInteger(VocationIndexHash, (int)newVocation.Vocation);

            if (newVocation.CombatAnimatorOverride != null)
            {
                animator.runtimeAnimatorController = newVocation.CombatAnimatorOverride;
            }
        }
    }
}
