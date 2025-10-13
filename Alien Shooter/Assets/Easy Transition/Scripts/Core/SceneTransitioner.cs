namespace PixeLadder.EasyTransition
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// A singleton manager that controls the entire scene transition process.
    /// </summary>
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance;

        [Header("Configuration")]
        [Tooltip("The screen-covering Image prefab used for transitions.")]
        [SerializeField] private Image transitionImagePrefab;
        [Tooltip("The default transition effect to use if none is provided in the LoadScene call.")]
        [SerializeField] private TransitionEffect defaultTransition;

        // --- Private State ---
        private Image transitionImageInstance;
        public static event System.Action OnSceneLoaded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Create a dedicated, persistent canvas for the transition UI.
            GameObject canvasGO = new GameObject("TransitionCanvas");
            canvasGO.transform.SetParent(this.transform);
            var transitionCanvas = canvasGO.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            transitionImageInstance = Instantiate(transitionImagePrefab, canvasGO.transform);
            transitionImageInstance.gameObject.SetActive(false);
        }

        /// <summary>
        /// The main public method to start a scene transition.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="effect">The TransitionEffect ScriptableObject defining the visuals.</param>
        public void LoadScene(string sceneName, TransitionEffect effect = null)
        {
            var effectToUse = effect ?? defaultTransition;
            if (effectToUse == null)
            {
                Debug.LogError("SceneTransitioner: No transition effect specified and no default is set.", this);
                return;
            }

            StopAllCoroutines();
            StartCoroutine(TransitionRoutine(sceneName, effectToUse));
        }

        private IEnumerator TransitionRoutine(string sceneName, TransitionEffect effect)
        {
            // Prepare the material and activate the image.
            transitionImageInstance.gameObject.SetActive(true);
            Material materialInstance = new Material(effect.transitionMaterial);
            transitionImageInstance.material = materialInstance;
            effect.SetEffectProperties(materialInstance);

            // Run the fade-out animation.
            yield return effect.AnimateOut(transitionImageInstance);

            // Load the new scene.
            yield return SceneManager.LoadSceneAsync(sceneName);

            // Fire the event to notify any listeners that the load is complete.
            OnSceneLoaded?.Invoke();

            // Run the fade-in animation.
            yield return effect.AnimateIn(transitionImageInstance);

            // Deactivate the image.
            transitionImageInstance.gameObject.SetActive(false);
        }
    }
}