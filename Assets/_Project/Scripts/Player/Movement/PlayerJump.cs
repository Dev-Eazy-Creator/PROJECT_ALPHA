// Manages jump input, vertical velocity, gravity, and ground detection.
using UnityEngine;

namespace ProjectAlpha
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerJump : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private float JumpHeight = 2f;
        [SerializeField] private float GravityScale = 2f;

        private CharacterController controller;
        private float verticalVelocity;
        private bool gravityEnabled = true;

        // A small downward bias keeps CharacterController.isGrounded stable while grounded.
        private const float GroundedStickVelocity = -2f;

        public bool IsGrounded => controller.isGrounded;
        public float VerticalVelocity => verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnJumpStarted += HandleJumpStarted;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnJumpStarted -= HandleJumpStarted;
            }
        }

        private void Update()
        {
            if (!gravityEnabled)
            {
                return;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = GroundedStickVelocity;
            }

            verticalVelocity += Physics.gravity.y * GravityScale * Time.deltaTime;
            motor.AddVelocity(new Vector3(0f, verticalVelocity, 0f));
        }

        private void HandleJumpStarted()
        {
            if (gravityEnabled && controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y * GravityScale);
            }
        }

        // Called by PlayerClimb when entering the climb state: gravity off, velocity zeroed.
        public void EnterClimbOverride()
        {
            gravityEnabled = false;
            verticalVelocity = 0f;
        }

        // Called by PlayerClimb when leaving the climb state: gravity resumes.
        public void ExitClimbOverride()
        {
            gravityEnabled = true;
            verticalVelocity = 0f;
        }
    }
}
