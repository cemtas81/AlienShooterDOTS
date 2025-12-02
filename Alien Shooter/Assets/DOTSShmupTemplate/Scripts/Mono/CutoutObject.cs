using UnityEngine;

namespace cemtas81
{
    public class CutoutObject : MonoBehaviour
    {
        public static int posId = Shader.PropertyToID("_Position");
        public static int sizeId = Shader.PropertyToID("_Size");
        public Material[] materials; // Array of materials to support multiple materials
        public Camera cam;
        public LayerMask layerMask;
        public float size;
        public float smoothSpeed = 5f; // Speed of the transition

        private float currentSize = 0f; // Current size value for smooth transition
        private RaycastHit[] hits = new RaycastHit[1]; // Pre-allocated array for raycast hits

        private void Update()
        {
            var dir = cam.transform.position - transform.position;
            var ray = new Ray(transform.position, dir.normalized);

            // Use RaycastNonAlloc to avoid memory allocation
            bool isHit = Physics.RaycastNonAlloc(ray, hits, 3000, layerMask) > 0;
            float targetSize = isHit ? size : 0f;

            // Smoothly interpolate the size value
            currentSize = Mathf.Lerp(currentSize, targetSize, smoothSpeed * Time.deltaTime);

            // Update materials using a for loop for better performance
            var view = cam.WorldToViewportPoint(transform.position);
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat(sizeId, currentSize);
                materials[i].SetVector(posId, view);
            }
        }
    }
}
