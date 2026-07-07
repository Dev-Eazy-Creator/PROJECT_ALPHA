// Drives the live character render for the inventory screen: enables the preview camera only while the
// screen is shown, and orbits the camera around the player (never rotates the gameplay player itself).
using UnityEngine;

namespace ProjectAlpha
{
    public class CharacterPreview : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [Tooltip("Point the camera orbits around (the player root). Rotating orbits the camera, not the player.")]
        [SerializeField] private Transform orbitPivot;
        [SerializeField] private float rotateSpeed = 0.3f;

        private void Awake() => SetActive(false);

        // The screen calls this on open/close so the camera only renders while visible.
        public void SetActive(bool active)
        {
            if (previewCamera != null)
            {
                previewCamera.enabled = active;
            }
        }

        // Orbits the camera horizontally by a pointer delta (see PreviewDragRotator).
        public void Rotate(float screenDeltaX)
        {
            if (previewCamera != null && orbitPivot != null)
            {
                previewCamera.transform.RotateAround(orbitPivot.position, Vector3.up, screenDeltaX * rotateSpeed);
            }
        }
    }
}
