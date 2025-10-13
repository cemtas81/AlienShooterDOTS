namespace PixeLadder.EasyTransition.Demo
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using TMPro;

    // This using directive allows us to use 'TransitionEffect' without the full namespace.
    using PixeLadder.EasyTransition;

    /// <summary>
    /// Manages the logic for the self-contained demo scene.
    /// It cycles through a list of TransitionEffects to demonstrate the asset's features.
    /// </summary>
    public class DemoSceneController : MonoBehaviour
    {
        #region Fields
        [Header("Scene References")]
        [Tooltip("The TextMeshPro element that displays the current scene's name.")]
        [SerializeField] private TMP_Text sceneNameText;
        [Tooltip("The TextMeshPro element inside the button, which displays the transition effect's name.")]
        [SerializeField] private TMP_Text buttonText;
        [Tooltip("The main button that triggers the scene transition.")]
        [SerializeField] private Button transitionButton;

        [Header("Demo Configuration")]
        [Tooltip("The array of TransitionEffect assets to cycle through in the demo.")]
        [SerializeField] private TransitionEffect[] transitionEffects;

        // A static variable's value persists across scene reloads,
        // allowing us to cycle through the effects.
        private static int currentSceneIndex = 0;
        #endregion

        #region Unity Lifecycle & Event Handling
        private void OnEnable()
        {
            // Subscribe to the event to know when the scene has finished loading.
            SceneTransitioner.OnSceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            // Always unsubscribe from events to prevent memory leaks.
            SceneTransitioner.OnSceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            UpdateUI();
            if (transitionButton != null)
            {
                transitionButton.onClick.AddListener(OnTransitionButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // It's good practice to remove listeners in OnDestroy as well.
            if (transitionButton != null)
            {
                transitionButton.onClick.RemoveListener(OnTransitionButtonClicked);
            }
        }
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Called when the main transition button is clicked.
        /// </summary>
        private void OnTransitionButtonClicked()
        {
            if (transitionEffects.Length == 0) return;

            // The "next scene" is just this same scene, which we will reload.
            string sceneToLoad = SceneManager.GetActiveScene().name;
            TransitionEffect effectToUse = transitionEffects[currentSceneIndex];

            // Call the SceneTransitioner to start the transition.
            SceneTransitioner.Instance.LoadScene(sceneToLoad, effectToUse);
        }

        /// <summary>
        /// This method is called by the OnSceneLoaded event from the SceneTransitioner.
        /// </summary>
        public void OnSceneLoaded()
        {
            if (transitionEffects.Length == 0) return;

            // Increment the static index to use the next effect in the list for the next transition.
            currentSceneIndex = (currentSceneIndex + 1) % transitionEffects.Length;
        }
        #endregion

        #region UI Logic
        /// <summary>
        /// Updates the text elements in the scene to reflect the current state.
        /// </summary>
        private void UpdateUI()
        {
            // Failsafe in case no effects are assigned in the Inspector.
            if (transitionEffects.Length == 0)
            {
                if (sceneNameText != null) sceneNameText.text = "Error";
                if (buttonText != null) buttonText.text = "No Effects Assigned";
                return;
            }

            // Update the UI based on the static index.
            if (sceneNameText != null) sceneNameText.text = $"Scene {currentSceneIndex + 1}";
            if (buttonText != null) buttonText.text = $"Load Next Scene (using {transitionEffects[currentSceneIndex].name})";

            // Your original logic to force the button's layout to update immediately.
            if (transitionButton != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transitionButton.GetComponent<RectTransform>());
            }
        }
        #endregion
    }
}