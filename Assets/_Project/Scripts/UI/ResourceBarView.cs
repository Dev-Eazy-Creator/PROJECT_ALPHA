// Binds a resource pool (health/stamina) to a uGUI bar, updating live on OnChanged.
// Supports a filled Image and/or a Slider, so it fits AriaGUI bars or a plain Image.
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAlpha
{
    public class ResourceBarView : MonoBehaviour
    {
        [Tooltip("An Image with Image Type = Filled (Horizontal). Optional.")]
        [SerializeField] private Image fillImage;
        [Tooltip("A uGUI Slider (e.g. an AriaGUI bar). Optional.")]
        [SerializeField] private Slider slider;

        private ResourcePool pool;

        // Called by PlayerHud once the controllers have created their pools.
        public void Bind(ResourcePool source)
        {
            if (pool != null)
            {
                pool.OnChanged -= HandleChanged;
            }

            pool = source;

            if (pool != null)
            {
                pool.OnChanged += HandleChanged;
                // Refresh immediately in case the pool was already initialized before we subscribed.
                HandleChanged(pool.Current, pool.Max);
            }
        }

        private void OnDestroy()
        {
            if (pool != null)
            {
                pool.OnChanged -= HandleChanged;
            }
        }

        private void HandleChanged(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = normalized;
            }
            if (slider != null)
            {
                slider.SetValueWithoutNotify(normalized);
            }
        }
    }
}
