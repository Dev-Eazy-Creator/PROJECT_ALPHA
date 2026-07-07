// Drives Animator parameters based on player state. Single source of truth for animation state.
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectAlpha
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerJump jump;
        [SerializeField] private PlayerClimb climb;
        [SerializeField] private PlayerMovement movement;
        [FormerlySerializedAs("vocationManager")]
        [SerializeField] private JobManager jobManager;

        private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsClimbingHash = Animator.StringToHash("isClimbing");
        private static readonly int JobIndexHash = Animator.StringToHash("jobIndex");
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
            if (jobManager != null)
            {
                jobManager.OnJobChanged += HandleJobChanged;
            }
        }

        private void OnDisable()
        {
            if (jobManager != null)
            {
                jobManager.OnJobChanged -= HandleJobChanged;
            }
        }

        private void Start()
        {
            // Apply the starting job's override controller and index.
            if (jobManager != null && jobManager.CurrentJob != null)
            {
                HandleJobChanged(jobManager.CurrentJob);
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

        private void HandleJobChanged(JobData newJob)
        {
            if (animator == null || newJob == null)
            {
                return;
            }

            animator.SetInteger(JobIndexHash, (int)newJob.Job);

            if (newJob.CombatAnimatorOverride != null)
            {
                animator.runtimeAnimatorController = newJob.CombatAnimatorOverride;
            }
        }
    }
}
