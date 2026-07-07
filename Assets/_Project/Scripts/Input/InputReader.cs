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
        public event Action OnToggleInventoryStarted;
        public event Action OnConfirmEquipStarted;

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction switchJobAction;
        private InputAction sprintAction;

        private InputActionMap uiMap;
        private InputAction toggleInventoryAction;
        private InputAction confirmEquipAction;

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

            // The UI map ships alongside Player. Defensive: it may be empty/absent in older assets.
            uiMap = actions.FindActionMap("UI", throwIfNotFound: false);
            if (uiMap != null)
            {
                toggleInventoryAction = uiMap.FindAction("ToggleInventory", throwIfNotFound: false);
                if (toggleInventoryAction != null)
                {
                    toggleInventoryAction.started += HandleToggleInventoryStarted;
                }

                confirmEquipAction = uiMap.FindAction("ConfirmEquip", throwIfNotFound: false);
                if (confirmEquipAction != null)
                {
                    confirmEquipAction.started += HandleConfirmEquipStarted;
                }

                uiMap.Enable();
            }
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

            if (uiMap != null)
            {
                if (toggleInventoryAction != null)
                {
                    toggleInventoryAction.started -= HandleToggleInventoryStarted;
                }

                if (confirmEquipAction != null)
                {
                    confirmEquipAction.started -= HandleConfirmEquipStarted;
                }

                uiMap.Disable();
                uiMap = null;
            }
        }

        // Lets the inventory screen suspend gameplay input while it's open, so E / gamepad A drive the
        // menu's equip action instead of also firing Interact / Jump.
        public void SetGameplayInputEnabled(bool value)
        {
            if (playerMap == null)
            {
                return;
            }

            if (value)
            {
                playerMap.Enable();
            }
            else
            {
                playerMap.Disable();
            }
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
        private void HandleToggleInventoryStarted(InputAction.CallbackContext ctx) => OnToggleInventoryStarted?.Invoke();
        private void HandleConfirmEquipStarted(InputAction.CallbackContext ctx) => OnConfirmEquipStarted?.Invoke();
    }
}
