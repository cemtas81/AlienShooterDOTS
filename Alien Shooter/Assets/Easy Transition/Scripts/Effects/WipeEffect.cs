namespace PixeLadder.EasyTransition.Effects
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A directional wipe transition effect with separate In/Out settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWipeEffect", menuName = "Easy Transition/Wipe Effect")]
    public class WipeEffect : TransitionEffect
    {
        public enum WipeDirection
        {
            LeftToRight, RightToLeft, BottomToTop, TopToBottom,
            BottomLeftToTopRight, BottomRightToTopLeft, TopLeftToBottomRight, TopRightToBottomLeft
        }

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = 1.0f;

        [Header("Wipe Properties")]
        [SerializeField] private WipeDirection fadeOutDirection = WipeDirection.LeftToRight;
        [SerializeField] private WipeDirection fadeInDirection = WipeDirection.RightToLeft;
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothness = 0.2f;

        private static readonly int AngleProperty = Shader.PropertyToID("_WipeAngle");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_WipeSmoothness");

        public override void SetEffectProperties(Material materialInstance)
        {
            materialInstance.SetFloat(SmoothnessProperty, smoothness);
        }

        public override IEnumerator AnimateOut(Image transitionImage)
        {
            transitionImage.material.SetFloat(AngleProperty, GetAngleForDirection(fadeOutDirection));
            // Fades from 0 (clear) to 1 (covered).
            yield return AnimateCutoff(transitionImage.material, 0f, 1f, duration / 2);
        }

        public override IEnumerator AnimateIn(Image transitionImage)
        {
            transitionImage.material.SetFloat(AngleProperty, GetAngleForDirection(fadeInDirection));
            // Fades from 1 (covered) to 0 (clear).
            yield return AnimateCutoff(transitionImage.material, 1f, 0f, duration / 2);
        }

        private float GetAngleForDirection(WipeDirection dir)
        {
            return dir switch
            {
                WipeDirection.LeftToRight => 0f * Mathf.Deg2Rad,
                WipeDirection.RightToLeft => 180f * Mathf.Deg2Rad,
                WipeDirection.BottomToTop => 90f * Mathf.Deg2Rad,
                WipeDirection.TopToBottom => 270f * Mathf.Deg2Rad,
                WipeDirection.BottomLeftToTopRight => 45f * Mathf.Deg2Rad,
                WipeDirection.BottomRightToTopLeft => 135f * Mathf.Deg2Rad,
                WipeDirection.TopLeftToBottomRight => 315f * Mathf.Deg2Rad,
                WipeDirection.TopRightToBottomLeft => 225f * Mathf.Deg2Rad,
                _ => 0f,
            };
        }
    }
}