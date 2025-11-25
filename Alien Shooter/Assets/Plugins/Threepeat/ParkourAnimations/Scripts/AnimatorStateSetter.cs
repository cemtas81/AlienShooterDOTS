using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Threepeat
{
    public class AnimatorStateSetter : MonoBehaviour
    {
        public AnimationClip stateToPlay;
        public bool resetOnComplete = false;
        public bool returnToInitialPositionOnReset = true;
        public bool sprintFirst = false;

        public float animationTimeOverride = -1f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private float currentAnimStartTime = 0f;

        private Animator animator;

        private float TIME_TO_PLAY = 10f;

        // Start is called before the first frame update
        void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            animator = GetComponent<Animator>();

            animator.Play(stateToPlay.name, -1, 0f);
            currentAnimStartTime = Time.time;

            TIME_TO_PLAY = stateToPlay.length;
            if (animationTimeOverride > 0)
            {
                TIME_TO_PLAY = animationTimeOverride;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (resetOnComplete)
            {
                //Debug.LogFormat("Current {0}, start {1}, len {2}", Time.time, currentAnimStartTime, stateToPlay.length);
                if ((Time.time - currentAnimStartTime) > TIME_TO_PLAY)
                {
                    if (returnToInitialPositionOnReset)
                    {
                        transform.position = initialPosition;
                        transform.rotation = initialRotation;
                    }
                    animator.Play(stateToPlay.name, -1, 0f);
                    currentAnimStartTime = Time.time;
                }
            }
        }
    }
}