// Manages climb state entry, exit, and vertical movement on climbable surfaces.
using UnityEngine;

namespace ProjectAlpha
{
    public class PlayerClimb : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerJump jump;
        [SerializeField] private float ClimbSpeed = 3f;

        private const string ClimbableTag = "Climbable";

        private bool isOverlappingClimbable;
        private Vector2 moveInput;

        public bool IsClimbing { get; private set; }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnMoveInput += HandleMoveInput;
                inputReader.OnInteractStarted += HandleInteractStarted;
                inputReader.OnJumpStarted += HandleJumpStarted;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnMoveInput -= HandleMoveInput;
                inputReader.OnInteractStarted -= HandleInteractStarted;
                inputReader.OnJumpStarted -= HandleJumpStarted;
            }
        }

        private void HandleMoveInput(Vector2 value)
        {
            moveInput = value;
        }

        private void HandleInteractStarted()
        {
            if (!IsClimbing)
            {
                if (isOverlappingClimbable)
                {
                    EnterClimb();
                }
            }
            else
            {
                // Interact again toggles climb off.
                ExitClimb();
            }
        }

        private void HandleJumpStarted()
        {
            if (IsClimbing)
            {
                ExitClimb();
            }
        }

        private void Update()
        {
            if (!IsClimbing)
            {
                return;
            }

            // Movement is restricted to the Y axis while climbing; X/Z is locked by PlayerMovement.
            motor.AddVelocity(new Vector3(0f, moveInput.y * ClimbSpeed, 0f));
        }

        private void EnterClimb()
        {
            IsClimbing = true;
            if (jump != null)
            {
                jump.EnterClimbOverride();
            }
        }

        private void ExitClimb()
        {
            IsClimbing = false;
            if (jump != null)
            {
                jump.ExitClimbOverride();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(ClimbableTag))
            {
                isOverlappingClimbable = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(ClimbableTag))
            {
                isOverlappingClimbable = false;

                // Leaving the surface always ends the climb.
                if (IsClimbing)
                {
                    ExitClimb();
                }
            }
        }
    }
}
