// Single owner of CharacterController.Move(). Aggregates velocity from all movement sources
// and locks the player to its 2.5D depth plane on the Z axis.
using UnityEngine;

namespace ProjectAlpha
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Hard-lock the player to its starting Z position so it can never drift off the sidescroller plane.")]
        [SerializeField] private bool lockZPlane = true;

        private CharacterController controller;
        private Vector3 accumulatedVelocity;
        private float lockedZ;

        // Horizontal (X) speed of the last applied move, for the animator.
        public float HorizontalSpeed { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            lockedZ = transform.position.z;
        }

        // Called each frame by movement contributors (movement, jump, climb).
        public void AddVelocity(Vector3 velocity)
        {
            accumulatedVelocity += velocity;
        }

        private void LateUpdate()
        {
            controller.Move(accumulatedVelocity * Time.deltaTime);

            // Guarantee the player stays on its depth plane, whatever pushed it (input, collisions, slopes).
            if (lockZPlane)
            {
                Vector3 position = transform.position;
                if (!Mathf.Approximately(position.z, lockedZ))
                {
                    position.z = lockedZ;
                    transform.position = position;
                }
            }

            HorizontalSpeed = Mathf.Abs(accumulatedVelocity.x);

            accumulatedVelocity = Vector3.zero;
        }
    }
}
