namespace PixeLadder.EasyTransition.Effects
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A transition effect that uses a Voronoi/Cellular noise pattern.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCellularEffect", menuName = "Easy Transition/Cellular Effect")]
    public class CellularEffect : TransitionEffect
    {
        public enum AnimationDirection { Reveal, Obscure }

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = 1.0f;

        [Header("Fade Out Settings")]
        [SerializeField] private AnimationDirection fadeOutAnimation = AnimationDirection.Obscure;
        [SerializeField] private bool invertFadeOut = false;

        [Header("Fade In Settings")]
        [SerializeField] private AnimationDirection fadeInAnimation = AnimationDirection.Reveal;
        [SerializeField] private bool invertFadeIn = false;

        [Header("Shared Settings")]
        [SerializeField, Min(1f)] private float cellDensity = 10.0f;
        [SerializeField, Min(0f)] private float cellSpeed = 15.0f;
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothness = 0.1f;

        private static readonly int InvertProperty = Shader.PropertyToID("_Invert");
        private static readonly int DensityProperty = Shader.PropertyToID("_CellDensity");
        private static readonly int SpeedProperty = Shader.PropertyToID("_CellSpeed");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_CellSmoothness");

        public override void SetEffectProperties(Material materialInstance)
        {
            materialInstance.SetFloat(SmoothnessProperty, smoothness);
            materialInstance.SetFloat(SpeedProperty, cellSpeed);
            materialInstance.SetFloat(DensityProperty, cellDensity);
        }

        public override IEnumerator AnimateOut(Image transitionImage)
        {
            transitionImage.material.SetFloat(InvertProperty, invertFadeOut ? 1.0f : 0.0f);
            float startValue = (fadeOutAnimation == AnimationDirection.Reveal) ? 0f : 1f;
            float endValue = (fadeOutAnimation == AnimationDirection.Reveal) ? 1f : 0f;
            yield return AnimateCutoff(transitionImage.material, startValue, endValue, duration / 2);
        }

        public override IEnumerator AnimateIn(Image transitionImage)
        {
            transitionImage.material.SetFloat(InvertProperty, invertFadeIn ? 1.0f : 0.0f);
            float startValue = (fadeInAnimation == AnimationDirection.Reveal) ? 0f : 1f;
            float endValue = (fadeInAnimation == AnimationDirection.Reveal) ? 1f : 0f;
            yield return AnimateCutoff(transitionImage.material, startValue, endValue, duration / 2);
        }
    }
}