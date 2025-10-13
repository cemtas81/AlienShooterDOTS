namespace PixeLadder.EasyTransition
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A helper component that automatically passes a UI Image's RectTransform size to its material.
    /// Essential for shaders that need to be aware of the UI element's dimensions.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public class UIMaterialPropertyUpdater : MonoBehaviour
    {
        private Image image;
        private RectTransform rectTransform;
        private Material materialInstance;

        // Cache the property ID for efficiency.
        private static readonly int RectSizeProperty = Shader.PropertyToID("_RectSize");

        private void OnEnable()
        {
            image = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();

            // Create a unique instance of the material to avoid modifying the shared asset.
            materialInstance = new Material(image.material);
            image.material = materialInstance;

            UpdateMaterialProperties();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateMaterialProperties();
        }

        private void Update()
        {
            // Fallback for editor updates
            if (Application.isEditor && !Application.isPlaying)
            {
                UpdateMaterialProperties();
            }
        }

        private void UpdateMaterialProperties()
        {
            if (materialInstance == null || rectTransform == null) return;
            Vector2 currentSize = rectTransform.rect.size;
            materialInstance.SetVector(RectSizeProperty, new Vector4(currentSize.x, currentSize.y, 0, 0));
        }

        private void OnDisable()
        {
            // Clean up the created material instance to prevent memory leaks in the editor.
            if (materialInstance != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                {
                    DestroyImmediate(materialInstance);
                }
                else
                {
                    Destroy(materialInstance);
                }
            }
        }
    }
}