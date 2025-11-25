using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Threepeat
{

    [CreateAssetMenu(fileName = "AnimClipDemoInfo", menuName = "Threepeat/AnimationClip Demo Info")]
    public class AnimationClipDemoInfo : ScriptableObject
    {
        [Multiline]
        public string animationName = "TYPE - sprint-to-sprint";
        public AnimationClip clip;
        public AnimationClip[] preExitClips = { };

        public string animatorStateName = "";
        public Vector3 obstacleFrontEdge = Vector3.zero;
        public Vector3 obstacleBackEdge = Vector3.zero;

        public Vector3 platformFrontEdge = Vector3.zero;
        public Vector3 platformBackEdge = Vector3.zero;

        public bool doLeftHandIK = false;
        public Vector3 leftHandIKOffset = Vector3.zero;
        public float fullIKThreshDist = 0.25f;

        public float animationDurationOverride = -1f;

        public bool rightWall = false;
        [Tooltip("-1 in Y means just match obstacle height, -1 in Z means go from character Z position to obstacle front edge")]
        public Vector3 rightWallXOffHeightLength = new Vector3(1f, -1f, -1f);

        public bool disappearObstacleImmediatelyAfterClip = false;

        public enum IKTargetLocation
        {
            Front,
            Back
        }

        public bool doRightHandIK = false;
        public Vector3 rightHandIKOffset = Vector3.zero;

        public float playerZForMaxIKWeight = -1;

        public IKTargetLocation IKTarget = IKTargetLocation.Front;

        public AnimationClip entryClip;
        public AnimationClip exitClip;

        public bool doCrossfade = true;

        public string GetClipName()
        {
            return (animatorStateName == "") ? clip.name : animatorStateName;
        }

        public bool HasObstacle()
        {
            return obstacleFrontEdge != obstacleBackEdge;
        }

        internal bool HasPlatform()
        {
            return platformFrontEdge != platformBackEdge;
        }
    }
}