using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Unity.Vehicles.Samples
{
    /// <summary>
    /// Generates skidmark mesh based on wheel slip values.
    /// One SkidmarkGenerator is used per wheel.
    /// </summary>
    public class SkidmarkGenerator : MonoBehaviour
    {
        /// <summary>
        /// Current skidmark color. The alpha channel will be ignored since that 
        /// is determined by the wheel slip.
        /// </summary>
        [UnityEngine.Tooltip("Current skidmark color. The alpha channel will be ignored since that  is determined by the wheel slip.")]
        public Color SkidColor = Color.black;

        /// <summary>
        /// The alpha value below which the skidmarks will not get drawn.
        /// </summary>
        [UnityEngine.Tooltip("The alpha value below which the skidmarks will not get drawn.")]
        public float ColorAlphaThreshold = 0.05f;

        /// <summary>
        /// Maximum alpha value that the skidmark can have, no matter the wheel slip.
        /// </summary>
        [UnityEngine.Tooltip("Maximum alpha value that the skidmark can have, no matter the wheel slip.")]
        public float PeakAlpha = 0.5f;

        /// <summary>
        /// Maximum amount of skidmark sections that can exist at the same time.
        /// After reaching the max value the skidmark loops around.
        /// Each section adds 2 vertices and 6 triangles to the mesh, so this setting is important
        /// for performance.
        /// </summary>
        [Range(2, 1024)]
        [UnityEngine.Tooltip("Maximum amount of skidmark sections that can exist at the same time. After reaching the max value the skidmark loops around. Each section adds 2 vertices and 6 triangles to the mesh, so this setting is important for performance.")]
        public int MaxSections = 128;

        /// <summary>
        /// The percentage of the skidmark tail that will be faded out when looping around.
        /// </summary>
        [Range(0f, 1f)]
        [UnityEngine.Tooltip("The percentage of the skidmark tail that will be faded out when looping around.")]
        public float TailFadePercent = 0.2f;

        /// <summary>
        /// Offset from the ground to prevent the skidmarks from being obscured by the ground surface.
        /// </summary>
        [Range(0f, 0.05f)]
        [UnityEngine.Tooltip("Offset from the ground to prevent the skidmarks from being obscured by the ground surface.")]
        public float GroundOffset = 0.025f;

        /// <summary>
        /// What distance needs to be covered by the wheel for a new section to be generated?
        /// </summary>
        [UnityEngine.Tooltip("What distance needs to be covered by the wheel for a new section to be generated?")]
        public float DistanceThreshold = 0.2f;

        // Mesh data
        private Mesh SkidMesh;
        private Vector3[] Vertices;
        private int[] Triangles;
        private Color[] Colors;

        // Calculated
        private Vector3 PrevPosition;
        private Vector3 LocalPositionLeft;
        private Vector3 LocalPositionRight;
        private Vector3 PrevLocalPositionLeft;
        private Vector3 PrevLocalPositionRight;
        private float Alpha;
        private float PrevAlpha;
        private float DistanceAccumulator;
        private int MaxTriangles;
        private int SectionIndex;
        private float FadeCoefficient;
        private int FadeLength;
        private Bounds MeshBounds;
        private int Vi0;
        private int Vi1;
        private int Vi2;
        private int Vi3;

        // Components
        private MeshFilter SkidmarkMeshFilter;


        private void Awake()
        {
            if (MaxSections < 4)
            {
                MaxSections = 4;
                Debug.LogWarning("Minimum recommended number of skidmark sections is 4.");
            }

            Alpha = 0f;
            PrevAlpha = 0f;

            MaxTriangles = MaxSections * 2;

            // Init mesh arrays
            Vertices = new Vector3[MaxSections * 4];
            Colors = new Color[MaxSections * 4];
            Triangles = new int[MaxSections * 6];

            // Create new mesh
            SkidMesh = new Mesh();
            SkidMesh.MarkDynamic();
            SkidMesh.name = "SkidmarkMesh";

            SkidmarkMeshFilter = GetComponent<MeshFilter>();
            if (SkidmarkMeshFilter == null)
            {
                Debug.LogError("Skidmark generator mesh filter is null. Skidmark generator object should contain a MeshFilter.");
                return;
            }

            SkidmarkMeshFilter.mesh = SkidMesh;

            FadeLength = Mathf.FloorToInt(MaxTriangles * TailFadePercent);
            if (FadeLength < 2)
            {
                FadeCoefficient = 1f;
            }
            else
            {
                FadeCoefficient = Mathf.Pow(0.01f, 1.0f / FadeLength);
            }
        }


        /// <summary>
        /// Adds a skidmark section. A new section will only be generated if alpha is above the threshold
        /// and the distance from the previous skidmark sections is above the DistanceThreshold.
        /// </summary>
        public void AddSkidmarkIfNeeded(in Vector3 position, in Vector3 normal, in Vector3 forward,
            in Vector3 right, in float width, float inAlpha)
        {
            if (SkidMesh == null || SkidmarkMeshFilter == null)
            {
                Debug.LogError("Skidmark generator has not been initialized.");
                return;
            }

            // Distance from the last skidmark section
            Vector3 positionDelta = position - PrevPosition;
            PrevPosition = position;
            // Square maginitude does not work well here because we are doing a sum later
            float distance = Vector3.Magnitude(positionDelta);
            DistanceAccumulator += distance;

            // Calculate local left and right positions
            Vector3 rightOffset = width * 0.5f * right;
            Vector3 upOffset = normal * GroundOffset;
            Vector3 positionLeft = position - rightOffset + upOffset;
            Vector3 positionRight = position + rightOffset + upOffset;
            LocalPositionLeft = transform.InverseTransformPoint(positionLeft);
            LocalPositionRight = transform.InverseTransformPoint(positionRight);

            // Only lerp while the alpha is increasing as the skidmarks are a result of tire
            // overheating, but immediately reduce the value when the input alpha drops off.
            // Also prevents the fade-out happening when the wheel lifts off the ground.
            inAlpha = Mathf.Clamp(inAlpha, 0f, PeakAlpha);
            PrevAlpha = Alpha;
            Alpha = inAlpha > Alpha ?
                Mathf.Lerp(Alpha, inAlpha, Time.deltaTime * 2f) :
                inAlpha;

            // Exit if alpha is not high enough, but set the previous positions and distance 
            // so that the drawing restarts when landing.
            if (Alpha < ColorAlphaThreshold)
            {
                PrevLocalPositionLeft = LocalPositionLeft;
                PrevLocalPositionRight = LocalPositionRight;
                DistanceAccumulator = DistanceThreshold;
                return;
            }

            // Check in which direction the mesh is being built so that the triangles 
            // can be always placed face-up
            bool isForwardDirection = Vector3.Dot(forward, positionDelta) >= 0f;

            // Stretch the last mark to the wheel position
            if (DistanceAccumulator < DistanceThreshold)
            {
                // Generating but no need to update yet, update the latest vertex positions
                // and wait for the next call
                Vertices[Vi0] = LocalPositionLeft;
                Vertices[Vi1] = LocalPositionRight;
                PrevLocalPositionLeft = LocalPositionLeft;
                PrevLocalPositionRight = LocalPositionRight;
                SkidMesh.vertices = Vertices;
                return;
            }
            DistanceAccumulator = 0;

            // Add skidmark section

            // Advance to next section (rectangle). Do this at the beginning so that the final
            // state is left at the current section index.
            SectionIndex++;

            // Wrap around
            if (SectionIndex >= MaxSections)
            {
                SectionIndex = 0;
            }

            // Calculate vert and tri indices from the sections
            int vertIndex = SectionIndex * 4;
            int triIndex = SectionIndex * 6;

            // Get vertex indices
            // Vertex at index 0 is always the head
            Vi0 = vertIndex + 3;
            Vi1 = vertIndex + 2;
            Vi2 = vertIndex + 1;
            Vi3 = vertIndex + 0;

            // Set vertices
            Vertices[Vi0] = LocalPositionLeft;
            Vertices[Vi1] = LocalPositionRight;
            Vertices[Vi2] = PrevLocalPositionLeft;
            Vertices[Vi3] = PrevLocalPositionRight;

            // Set colors
            Color initialColor = SkidColor;
            initialColor.a = PrevAlpha;
            Color finalColor = SkidColor;
            finalColor.a = Alpha;

            // Build triangles and assign colors
            if (!isForwardDirection)
            {
                Triangles[triIndex + 0] = Vi0;
                Triangles[triIndex + 1] = Vi1;
                Triangles[triIndex + 2] = Vi2;

                Triangles[triIndex + 3] = Vi1;
                Triangles[triIndex + 4] = Vi3;
                Triangles[triIndex + 5] = Vi2;

                Colors[Vi0] = finalColor;
                Colors[Vi1] = finalColor;
                Colors[Vi2] = initialColor;
                Colors[Vi3] = initialColor;
            }
            else
            {
                Triangles[triIndex + 0] = Vi2;
                Triangles[triIndex + 1] = Vi1;
                Triangles[triIndex + 2] = Vi0;

                Triangles[triIndex + 3] = Vi2;
                Triangles[triIndex + 4] = Vi3;
                Triangles[triIndex + 5] = Vi1;

                Colors[Vi0] = initialColor;
                Colors[Vi1] = initialColor;
                Colors[Vi2] = finalColor;
                Colors[Vi3] = finalColor;
            }


            // Fade-out
            int fadeStartSectionIndex = SectionIndex + 1;
            int lastIndex = FadeLength - 1;
            for (int sectionIndex = 0; sectionIndex <= lastIndex; sectionIndex++)
            {
                int wrappedSectionIndex = (fadeStartSectionIndex + sectionIndex) % MaxSections;
                int fadeVertIndex = wrappedSectionIndex * 4;

                Colors[fadeVertIndex + 0].a *= FadeCoefficient;
                Colors[fadeVertIndex + 1].a *= FadeCoefficient;

                if (sectionIndex != lastIndex)
                {
                    Colors[fadeVertIndex + 2].a *= FadeCoefficient;
                    Colors[fadeVertIndex + 3].a *= FadeCoefficient;
                }
            }

            // Extend the bounds, if needed.
            MeshBounds.extents = new Vector3(
                    Mathf.Max(MeshBounds.extents.x, Mathf.Abs(position.x - transform.position.x)),
                    Mathf.Max(MeshBounds.extents.y, Mathf.Abs(position.y - transform.position.y)),
                    Mathf.Max(MeshBounds.extents.z, Mathf.Abs(position.z - transform.position.z))
                );

            // Assign new values
            SkidMesh.vertices = Vertices;
            SkidMesh.triangles = Triangles;
            SkidMesh.colors = Colors;
            SkidMesh.bounds = MeshBounds;

            // Remember current data for next section
            PrevLocalPositionLeft = LocalPositionLeft;
            PrevLocalPositionRight = LocalPositionRight;
        }
    }
}