namespace PixeLadder.EasyTransition.Effects
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A transition effect that dissolves the screen into pixels.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPixelateEffect", menuName = "Easy Transition/Pixelate Effect")]
    public class PixelateEffect : TransitionEffect
    {
        public enum AnimationDirection { Reveal, Obscure }

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = 1.0f;

        [Header("Shared Pixel Settings")]
        [SerializeField, Min(1f)] private float pixelDensity = 50f;
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothness = 0.2f;

        private static readonly int InvertProperty = Shader.PropertyToID("_Invert");
        private static readonly int DensityProperty = Shader.PropertyToID("_PixelDensity");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_Smoothness");

        public override void SetEffectProperties(Material materialInstance)
        {
            materialInstance.SetFloat(SmoothnessProperty, smoothness);
            materialInstance.SetFloat(DensityProperty, pixelDensity);
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