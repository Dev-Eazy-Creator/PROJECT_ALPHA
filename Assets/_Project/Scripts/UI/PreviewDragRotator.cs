// Optional: drag on the preview image to orbit the character (see CharacterPreview.Rotate).
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectAlpha
{
    public class PreviewDragRotator : MonoBehaviour, IDragHandler
    {
        [SerializeField] private CharacterPreview preview;

        public void OnDrag(PointerEventData eventData)
        {
            if (preview != null)
            {
                preview.Rotate(eventData.delta.x);
            }
        }
    }
}
