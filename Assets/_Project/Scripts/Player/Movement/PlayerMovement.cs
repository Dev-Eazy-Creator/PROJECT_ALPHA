// Reads player input, drives CharacterController movement on the X axis (walk/sprint),
// and turns the model to face the movement direction.
using UnityEngine;

namespace ProjectAlpha
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerClimb climb;
        [SerializeField] private PlayerJump jump;
        [Tooltip("Optional. When assigned, base walk speed comes from the MoveSpeed attribute instead of the field below.")]
        [SerializeField] private CharacterStats stats;
        [SerializeField] private float MoveSpeed = 2.5f;
        [Tooltip("Speed multiplier applied while sprinting (hold Shift / L3).")]
        [SerializeField] private float SprintMultiplier = 2f;

        [Header("Facing")]
        [Tooltip("The visual model to rotate. Auto-resolves to the child with the Animator if left empty.")]
        [SerializeField] private Transform modelTransform;
        [Tooltip("Model Y rotation when moving right (+X). Matches the sidescroller plane.")]
        [SerializeField] private float rightYaw = 90f;
        [Tooltip("Model Y rotation when moving left (-X).")]
        [SerializeField] private float leftYaw = -90f;
        [Tooltip("Degrees per second the model turns. 0 = instant snap.")]
        [SerializeField] private float turnSpeed = 900f;

        private Vector2 moveInput;
        private bool sprintHeld;
        private bool wasGrounded = true;
        private bool sprintCarriedIntoAir; // whether the player was sprinting when it left the ground
        private float targetYaw;

        // Future stamina system flips this off to forbid sprinting (e.g. when stamina is empty).
        public bool SprintAllowed { get; set; } = true;

        // True only while actually sprinting and moving — the hook a stamina drain will read.
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            if (jump == null)
            {
                jump = GetComponent<PlayerJump>();
            }

            if (modelTransform == null)
            {
                Animator modelAnimator = GetComponentInChildren<Animator>(true);
                if (modelAnimator != null)
                {
                    modelTransform = modelAnimator.transform;
                }
            }

            targetYaw = rightYaw;
            if (modelTransform != null)
            {
                modelTransform.localRotation = Quaternion.Euler(0f, targetYaw, 0f);
            }
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnMoveInput += HandleMoveInput;
                inputReader.OnSprintStarted += HandleSprintStarted;
                inputReader.OnSprintCanceled += HandleSprintCanceled;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnMoveInput -= HandleMoveInput;
                inputReader.OnSprintStarted -= HandleSprintStarted;
                inputReader.OnSprintCanceled -= HandleSprintCanceled;
            }
        }

        private void HandleMoveInput(Vector2 value)
        {
            moveInput = value;
        }

        private void HandleSprintStarted()
        {
            sprintHeld = true;
        }

        private void HandleSprintCanceled()
        {
            sprintHeld = false;
        }

        private void Update()
        {
            UpdateFacing();

            bool grounded = jump == null || jump.IsGrounded;

            // Latch the sprint state at the instant of takeoff: a running jump keeps its speed,
            // but pressing sprint once already airborne can never start a sprint.
            if (wasGrounded && !grounded)
            {
                sprintCarriedIntoAir = sprintHeld && SprintAllowed;
            }
            wasGrounded = grounded;

            // Vertical movement (jump/climb) is owned by other contributors.
            if (climb != null && climb.IsClimbing)
            {
                IsSprinting = false;
                return;
            }

            bool moving = Mathf.Abs(moveInput.x) > 0.01f;
            bool sprintEligible = grounded || sprintCarriedIntoAir;
            IsSprinting = sprintHeld && SprintAllowed && moving && sprintEligible;

            // Sidescroller: horizontal movement is locked to the X axis only.
            // Vertical input (W/S, Left Stick Y) is reserved for climbing — it must not drive world Z.
            // The X component still carries analog stick strength (0..1); WASD is normalized to 1.
            // Base speed comes from the MoveSpeed attribute when stats are wired; otherwise the raw field.
            float baseSpeed = stats != null ? stats.GetValue(AttributeType.MoveSpeed) : MoveSpeed;
            float speed = IsSprinting ? baseSpeed * SprintMultiplier : baseSpeed;
            Vector3 direction = new Vector3(moveInput.x, 0f, 0f);
            motor.AddVelocity(direction * speed);
        }

        private void UpdateFacing()
        {
            if (modelTransform == null)
            {
                return;
            }

            // Keep the last facing when there is no horizontal input.
            if (moveInput.x > 0.01f)
            {
                targetYaw = rightYaw;
            }
            else if (moveInput.x < -0.01f)
            {
                targetYaw = leftYaw;
            }

            Quaternion target = Quaternion.Euler(0f, targetYaw, 0f);
            modelTransform.localRotation = turnSpeed <= 0f
                ? target
                : Quaternion.RotateTowards(modelTransform.localRotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
