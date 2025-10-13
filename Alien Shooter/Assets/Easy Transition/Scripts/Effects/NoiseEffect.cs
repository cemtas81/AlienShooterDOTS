namespace PixeLadder.EasyTransition.Effects
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A transition effect that uses a scrolling Perlin/Simple noise pattern.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNoiseEffect", menuName = "Easy Transition/Noise Effect")]
    public class NoiseEffect : TransitionEffect
    {
        public enum AnimationDirection { Reveal, Obscure }

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = 1.0f;

        [Header("Shared Noise Settings")]
        [SerializeField, Min(0.1f)] private float noiseDensity = 1.0f;
        [SerializeField, Min(0f)] private float scrollSpeed = 0.3f;
        [SerializeField, Range(0f, 360f)] private float scrollAngle = 0f;
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothness = 0.2f;

        private static readonly int InvertProperty = Shader.PropertyToID("_Invert");
        private static readonly int DensityProperty = Shader.PropertyToID("_NoiseDensity");
        private static readonly int ScrollDirectionProperty = Shader.PropertyToID("_ScrollDirection");
        private static readonly int SpeedProperty = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_NoiseSmoothness");

        public override void SetEffectProperties(Material materialInstance)
        {
            materialInstance.SetFloat(SmoothnessProperty, smoothness);
            materialInstance.SetFloat(DensityProperty, noiseDensity);
            materialInstance.SetFloat(SpeedProperty, scrollSpeed);
            materialInstance.SetFloat(ScrollDirectionProperty, scrollAngle);
        }

        public override IEnumerator AnimateOut(Image transitionImage)
        {
            transitionImage.material.SetFloat(InvertProperty, 1.0f);
            float startValue = 0f;
            float endValue = 1f;
            yield return AnimateCutoff(transitionImage.material, startValue, endValue, duration / 2);
        }

        public override IEnumerator AnimateIn(Image transitionImage)
        {
            transitionImage.material.SetFloat(InvertProperty, 0.0f);
            float startValue = 0f;
            float endValue = 1f;
            yield return AnimateCutoff(transitionImage.material, startValue, endValue, duration / 2);
        }
    }
}