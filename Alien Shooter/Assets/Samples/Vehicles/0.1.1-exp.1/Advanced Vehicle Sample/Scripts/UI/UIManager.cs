using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Vehicles.Samples
{
    /// <summary>
    /// Vehicle UI settings.
    /// Also refer to <see cref="VehicleUISystem"/>.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// UIManager singleton instance.
        /// </summary>
        [Tooltip("UIManager singleton instance.")]
        public static UIManager Instance;

        /// <summary>
        /// Vehicle UIDocument.
        /// </summary>
        [Tooltip("Vehicle UIDocument.")]
        public UIDocument UIDocument;

        /// <summary>
        /// Vehicle speed Label.
        /// </summary>
        [Tooltip("Vehicle speed Label.")]
        public Label SpeedLabel;

        /// <summary>
        /// Engine RPM Label.
        /// </summary>
        [Tooltip("Engine RPM Label.")]
        public Label RPMLabel;

        /// <summary>
        /// Engine torque Label.
        /// </summary>
        [Tooltip("Engine torque Label.")]
        public Label TorqueLabel;

        /// <summary>
        /// Engine gear label. 
        /// </summary>
        [Tooltip("Engine gear label.")]
        public Label GearLabel;

        void OnEnable()
        {
            // Assign the labels
            SpeedLabel = UIDocument.rootVisualElement.Q<VisualElement>("Speed").Q<Label>("Value");
            RPMLabel = UIDocument.rootVisualElement.Q<VisualElement>("RPM").Q<Label>("Value");
            TorqueLabel = UIDocument.rootVisualElement.Q<VisualElement>("Torque").Q<Label>("Value");
            GearLabel = UIDocument.rootVisualElement.Q<VisualElement>("Gear").Q<Label>("Value");

            Instance = this;
        }

        void OnDisable()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void StaticReset()
        {
            Instance = null;
        }
    }
}
