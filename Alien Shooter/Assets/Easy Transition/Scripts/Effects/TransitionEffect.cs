namespace PixeLadder.EasyTransition
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public abstract class TransitionEffect : ScriptableObject
    {
        public Material transitionMaterial;

        public abstract IEnumerator AnimateOut(Image transitionImage);
        public abstract IEnumerator AnimateIn(Image transitionImage);
        public virtual void SetEffectProperties(Material materialInstance) { }

        /// <summary>
        /// A protected helper method to animate the material's _Cutoff property over time.
        /// </summary>
        protected IEnumerator AnimateCutoff(Material materialInstance, float startValue, float targetValue, float duration)
        {
            // This method now correctly accepts a startValue.
            materialInstance.SetFloat("_Cutoff", startValue);
            float time = 0;
            while (time < duration)
            {
                float cutoffValue = Mathf.Lerp(startValue, targetValue, time / duration);
                materialInstance.SetFloat("_Cutoff", cutoffValue);
                time += Time.deltaTime;
                yield return null;
            }
            materialInstance.SetFloat("_Cutoff", targetValue);
        }
    }
}