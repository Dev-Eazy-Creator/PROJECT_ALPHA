// Central input event broadcaster. All gameplay scripts subscribe here.
// No gameplay script ever holds a direct reference to PlayerInput or the raw action asset.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAlpha
{
    [CreateAssetMenu(fileName = "InputReader_Data", menuName = "Game/Input Reader")]
    public class InputReader : ScriptableObject
    {
        [Tooltip("Assign PlayerControls.inputactions here.")]
        [SerializeField] private InputActionAsset actions;

        public event Action<Vector2> OnMoveInput;
        public event Action OnJumpStarted;
        public event Action OnJumpCanceled;
        public event Action OnInteractStarted;
        public event Action OnInteractCanceled;
        public event Action OnSwitchJobStarted;
        public event Action OnSprintStarted;
        public event Action OnSprintCanceled;

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction switchJobAction;
        private InputAction sprintAction;

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogWarning("[InputReader] No InputActionAsset assigned. Input will not fire.");
                return;
            }

            playerMap = actions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
            jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
            interactAction = playerMap.FindAction("Interact", throwIfNotFound: true);
            switchJobAction = playerMap.FindAction("SwitchJob", throwIfNotFound: true);
            // Defensive: Sprint may be absent if the asset predates this action / hasn't reimported.
            sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: false);

            moveAction.performed += HandleMove;
            moveAction.canceled += HandleMove;
            jumpAction.started += HandleJumpStarted;
            jumpAction.canceled += HandleJumpCanceled;
            interactAction.started += HandleInteractStarted;
            interactAction.canceled += HandleInteractCanceled;
            switchJobAction.started += HandleSwitchJobStarted;
            if (sprintAction != null)
            {
                sprintAction.started += HandleSprintStarted;
                sprintAction.canceled += HandleSprintCanceled;
            }

            playerMap.Enable();
        }

        private void OnDisable()
        {
            if (playerMap == null)
            {
                return;
            }

            moveAction.performed -= HandleMove;
            moveAction.canceled -= HandleMove;
            jumpAction.started -= HandleJumpStarted;
            jumpAction.canceled -= HandleJumpCanceled;
            interactAction.started -= HandleInteractStarted;
            interactAction.canceled -= HandleInteractCanceled;
            switchJobAction.started -= HandleSwitchJobStarted;
            if (sprintAction != null)
            {
                sprintAction.started -= HandleSprintStarted;
                sprintAction.canceled -= HandleSprintCanceled;
            }

            playerMap.Disable();
            playerMap = null;
        }

        // Fires on performed and canceled so gameplay always sees the latest vector (zero on release).
        private void HandleMove(InputAction.CallbackContext ctx) => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
        private void HandleJumpStarted(InputAction.CallbackContext ctx) => OnJumpStarted?.Invoke();
        private void HandleJumpCanceled(InputAction.CallbackContext ctx) => OnJumpCanceled?.Invoke();
        private void HandleInteractStarted(InputAction.CallbackContext ctx) => OnInteractStarted?.Invoke();
        private void HandleInteractCanceled(InputAction.CallbackContext ctx) => OnInteractCanceled?.Invoke();
        private void HandleSwitchJobStarted(InputAction.CallbackContext ctx) => OnSwitchJobStarted?.Invoke();
        private void HandleSprintStarted(InputAction.CallbackContext ctx) => OnSprintStarted?.Invoke();
        private void HandleSprintCanceled(InputAction.CallbackContext ctx) => OnSprintCanceled?.Invoke();
    }
}
