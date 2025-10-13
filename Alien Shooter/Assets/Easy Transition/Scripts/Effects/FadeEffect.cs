namespace PixeLadder.EasyTransition.Effects
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A simple fade-to-black transition effect.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFadeEffect", menuName = "Easy Transition/Fade Effect")]
    public class FadeEffect : TransitionEffect
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = 1.0f;

        // This effect has no unique properties, so SetEffectProperties is not needed.

        public override IEnumerator AnimateOut(Image transitionImage)
        {
            // Fades from 0 (clear) to 1 (covered).
            yield return AnimateCutoff(transitionImage.material, 0f, 1f, duration / 2);
        }

        public override IEnumerator AnimateIn(Image transitionImage)
        {
            // Fades from 1 (covered) to 0 (clear).
            yield return AnimateCutoff(transitionImage.material, 1f, 0f, duration / 2);
        }
    }
}