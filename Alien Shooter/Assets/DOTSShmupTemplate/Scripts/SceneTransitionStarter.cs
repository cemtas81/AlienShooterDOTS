using UnityEngine;
using UnityEngine.SceneManagement;
using PixeLadder.EasyTransition;

public class SceneTransitionStarter : MonoBehaviour
{
    [SerializeField] 
    private TransitionEffect openingTransitionEffect;
    string currentScene;
    private void Start()
    {
        if (openingTransitionEffect != null)
        {
            // Mevcut sahneyi tekrar yükleyerek transition effect'i baþlat
            currentScene = SceneManager.GetActiveScene().name;
            SceneTransitioner.Instance.LoadScene(currentScene, openingTransitionEffect);
        }
    }

    private void OnEnable()
    {
        // Scene yüklendiðinde haberdar olmak için event'e abone ol
        SceneTransitioner.OnSceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Memory leak'i önlemek için event aboneliðini kaldýr
        SceneTransitioner.OnSceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded()
    {
        // Scene yüklendiðinde yapýlacak ekstra iþlemler buraya eklenebilir
      
    }
}