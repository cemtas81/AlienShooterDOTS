namespace PixeLadder.EasyTransition.Effects
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    [CreateAssetMenu(fileName = "NewCircleEffect", menuName = "Easy Transition/Circle Effect")]
    public class CircleEffect : TransitionEffect
    {
        public enum AnimationDirection { CenterToEdge, EdgeToCenter }

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = 1.0f;
        [Header("Fade Out Settings")]
        [SerializeField] private AnimationDirection fadeOutAnimation = AnimationDirection.CenterToEdge;
        [SerializeField] private bool invertFadeOut = false;
        [Header("Fade In Settings")]
        [SerializeField] private AnimationDirection fadeInAnimation = AnimationDirection.EdgeToCenter;
        [SerializeField] private bool invertFadeIn = false;
        [Header("Shared Settings")]
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothness = 0.1f;

        private static readonly int InvertProperty = Shader.PropertyToID("_Invert");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_CircleSmoothness");

        public override void SetEffectProperties(Material materialInstance)
        {
            materialInstance.SetFloat(SmoothnessProperty, smoothness);
        }

        public override IEnumerator AnimateOut(Image transitionImage)
        {
            transitionImage.material.SetFloat(InvertProperty, invertFadeOut ? 1.0f : 0.0f);
            float startValue = (fadeOutAnimation == AnimationDirection.CenterToEdge) ? 0f : 1f;
            float endValue = (fadeOutAnimation == AnimationDirection.CenterToEdge) ? 1f : 0f;
            // This call is now valid.
            yield return AnimateCutoff(transitionImage.material, startValue, endValue, duration / 2);
        }

        public override IEnumerator AnimateIn(Image transitionImage)
        {
            transitionImage.material.SetFloat(InvertProperty, invertFadeIn ? 1.0f : 0.0f);
            float startValue = (fadeInAnimation == AnimationDirection.CenterToEdge) ? 0f : 1f;
            float endValue = (fadeInAnimation == AnimationDirection.CenterToEdge) ? 1f : 0f;
            // This call is now valid.
            yield return AnimateCutoff(transitionImage.material, startValue, endValue, duration / 2);
        }
    }
}