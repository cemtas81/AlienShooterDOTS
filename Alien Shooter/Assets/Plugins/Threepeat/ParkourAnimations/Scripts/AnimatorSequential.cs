using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Threepeat
{
    public class AnimatorSequential : MonoBehaviour
    {
        private Animator animator;

        /*
        public string[] clipStateNames = { };
        public AnimationClip[] clips = { };*/


        public AnimationClipDemoInfo[] clipInfos = { };

        public GameObject obstacleObject;
        public GameObject platformObject;
        public GameObject rightwallObject;

        public bool autoplayOnRun = true;

        public string stateToPlayOnStop = "RunFwdStop";
        public TextMeshProUGUI textObject;

        float goalZ = 0;
        Vector3 handGoalPos;
        bool doingIK = false;
        AnimationClipDemoInfo currentClip;

        private bool currentlyPlaying = false;

        // Start is called before the first frame update
        void Start()
        {
            animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            if (autoplayOnRun && !currentlyPlaying)
            {
                StartCoroutine(PlayClips());
                currentlyPlaying = true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                StartCoroutine(PlayClip(0, !Input.GetKey(KeyCode.LeftShift), !Input.GetKey(KeyCode.RightShift)));
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                StartCoroutine(PlayClip(1, !Input.GetKey(KeyCode.LeftShift), true));
            }

            if (Input.GetKeyDown(KeyCode.Space) && !currentlyPlaying)
            {
                StartCoroutine(PlayClips());
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.LookRotation(Vector3.forward);
            }

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                if (Time.timeScale != 1f)
                {
                    Time.timeScale = 1f;
                }
                else
                {
                    Time.timeScale = 0.25f;
                }
            }
        }

        public IEnumerator PlayClips()
        {

            /*for (int ii=0; ii < clips.Length; ii++)
            {
                Debug.LogFormat("PLAYING1: {0} dur {1}", clipStateNames[ii], clips[ii].length);
                animator.Play(clipStateNames[ii]);
                yield return new WaitForSeconds(clips[ii].length);
            }*/

            for (int ii = 0; ii < clipInfos.Length; ii++)
            {
                yield return PlayClip(ii, false, clipInfos[ii].doCrossfade);
            }

            animator.CrossFadeInFixedTime(stateToPlayOnStop, 0.25f); //"Idle");

            currentlyPlaying = false;
            yield return null;
        }

        IEnumerator MakeObjectDisappear(GameObject obj, float duration, bool sendPlayerBackToGround = false, float delayStart = 0f)
        {
            if (delayStart > 0)
            {
                yield return new WaitForSeconds(delayStart);
            }
            Vector3 startPos = obj.transform.position;
            float height = obj.transform.localScale.y;
            Vector3 endPos = new Vector3(startPos.x, -height, startPos.z);
            float t = 0;

            float playerStartY = transform.position.y;

            while (t < 1)
            {
                obj.transform.position = Vector3.Lerp(startPos, endPos, t);
                if (sendPlayerBackToGround)
                {
                    transform.position = new Vector3(transform.position.x, Mathf.Lerp(playerStartY, 0, t), transform.position.z);
                    //Debug.LogFormat("Sending Player back to 0 {0}", transform.position.y);
                }
                t += Time.deltaTime / duration;
                yield return null;
            }
            obj.transform.position = endPos;
            yield return 0;
        }

        IEnumerator MakeObjectAppear(GameObject obj, Vector3 finalPosition, float duration)
        {
            Vector3 startPos = obj.transform.position;
            //float height = obj.transform.localScale.y;
            Vector3 endPos = finalPosition;
            //Vector3 endPos = new Vector3(startPos.x, 0f, startPos.z);

            float t = 0;
            while (t < 1)
            {
                obj.transform.position = Vector3.Lerp(startPos, endPos, t);
                t += Time.deltaTime / duration;
                yield return null;
            }
            obj.transform.position = endPos;
            yield return 0;
        }

        IEnumerator MakeObjectAndCharacterAppear(GameObject obj, Vector3 finalPosition, float playerYOffset, float duration, bool moveCharacter = true)
        {
            Vector3 startPos = obj.transform.position;
            //float height = obj.transform.localScale.y;
            Vector3 endPos = finalPosition;
            float t = 0;
            float currPlayerY;
            while (t < 1)
            {
                obj.transform.position = Vector3.Lerp(startPos, endPos, t);
                //currPlayerY = Mathf.Max(obj.transform.position.y + (obj.transform.localScale.y/2f) /*+ playerYOffset*/, 0f);
                currPlayerY = Mathf.Max(obj.transform.position.y + (obj.transform.localScale.y / 2f) /*+ playerYOffset*/, 0f);

                if (moveCharacter)
                {
                    this.transform.position = new Vector3(0f /*this.transform.position.x*/, currPlayerY, this.transform.position.z);
                    //Debug.LogFormat("moveChar instream {0}", transform.position.y);
                }
                t += Time.deltaTime / duration;
                yield return null;
            }
            obj.transform.position = endPos;
            if (moveCharacter)
            {
                transform.position = new Vector3(0f /*this.transform.position.x*/, obj.transform.localScale.y / 2f, this.transform.position.z);
                //Debug.LogFormat("moveChar end {0}", transform.position.y);
            }
            yield return 0;
        }


        IEnumerator PlayClip(int index, bool playIdleOnComplete = false, bool crossfade = false)
        {
            transform.rotation.SetLookRotation(Vector3.forward);
            if (clipInfos[index].entryClip != null)
            {
                Debug.LogFormat("PLAYING2: {0} dur {1}", clipInfos[index].entryClip.name, clipInfos[index].entryClip.length);
                if (crossfade)
                {
                    animator.CrossFadeInFixedTime(clipInfos[index].entryClip.name, 0.25f);
                    yield return new WaitForSeconds(0.25f);
                }
                else
                {
                    animator.Play(clipInfos[index].entryClip.name);
                }
            }

            if (clipInfos[index].HasPlatform())
            {
                Vector3 back = clipInfos[index].platformBackEdge;
                Vector3 front = clipInfos[index].platformFrontEdge;
                float depth = Mathf.Abs(back.z - front.z);
                float zCenter = transform.position.z + front.z + depth / 2f;
                float height = clipInfos[index].platformFrontEdge.y;
                //TODO: smooth interp player and platform

                platformObject.transform.position = new Vector3(0f, -height /*transform.position.y - 2 * height*/, zCenter);
                platformObject.SetActive(true);
                platformObject.transform.localScale = new Vector3(platformObject.transform.localScale.x, height * 2f, depth);
                /*yield return*/
                if ((rightwallObject != null) && clipInfos[index].rightWall)
                {
                    rightwallObject.SetActive(true);
                }
                else
                {
                    rightwallObject.SetActive(false);
                }
                StartCoroutine(MakeObjectAndCharacterAppear(
                       platformObject,
                       new Vector3(0f, 0f /*height / 2f*/, zCenter), height, 0.5f));
                //Mathf.Max(0.25f, clipInfos[index].entryClip != null ? clipInfos[index].entryClip.length : 0.25f))); //1f));
                /*transform.position = new Vector3(0, clipInfos[index].platformFrontEdge.y, transform.position.z);

                platformObject.transform.position = new Vector3(0f, transform.position.y - height, zCenter);
                platformObject.transform.localScale = new Vector3(platformObject.transform.localScale.x, height * 2f, depth);*/
            }
            else
            {
                platformObject.SetActive(false);
                //TODO: smooth interp player and platform
                transform.position = new Vector3(0, 0f, transform.position.z);
            }
            if (clipInfos[index].HasObstacle())
            {
                Vector3 back = clipInfos[index].obstacleBackEdge;
                Vector3 front = clipInfos[index].obstacleFrontEdge;

                float depth = Mathf.Abs(back.z - front.z);

                float height = clipInfos[index].obstacleFrontEdge.y;

                //obstacleObject.transform.position = new Vector3(0f, transform.position.y, transform.position.z + front.z);
                obstacleObject.transform.localScale = new Vector3(obstacleObject.transform.localScale.x, height * 2f, depth);
                obstacleObject.transform.position = new Vector3(0f, /*transform.position.y - height*1.1f*/ -height * 1.1f, transform.position.z + front.z + depth / 2f);
                //            yield return MakeObjectAppear(obstacleObject, obstacleObject.transform.position = new Vector3(0f, transform.position.y, transform.position.z + front.z), 1f);
                StartCoroutine(MakeObjectAndCharacterAppear(
                        obstacleObject,
                        new Vector3(0f, 0f, transform.position.z + front.z + depth / 2f), 0f, Mathf.Min(0.5f, Mathf.Max(0.25f, clipInfos[index].entryClip != null ? clipInfos[index].entryClip.length : 0.25f)), false));

            }

            if (clipInfos[index].entryClip != null)
            {
                yield return new WaitForSeconds(clipInfos[index].entryClip.length);
            }
            textObject.SetText(clipInfos[index].animationName);
            Debug.LogFormat("PLAYING2: {0} dur {1}", clipInfos[index].GetClipName(), clipInfos[index].clip.length);
            /*if (false) //crossfade)
            {
                animator.CrossFadeInFixedTime(clipInfos[index].GetClipName(), 0.15f);
            }
            else*/
            {
                animator.Play(clipInfos[index].GetClipName());
            }
            if (clipInfos[index].animationDurationOverride > 0)
            {
                yield return new WaitForSeconds(clipInfos[index].animationDurationOverride);
            }
            else if (clipInfos[index].doLeftHandIK || clipInfos[index].doRightHandIK)
            {
                goalZ = transform.position.z;
                goalZ += (clipInfos[index].playerZForMaxIKWeight != -1) ? clipInfos[index].playerZForMaxIKWeight : clipInfos[index].obstacleFrontEdge.z;
                if (clipInfos[index].IKTarget == AnimationClipDemoInfo.IKTargetLocation.Front)
                {
                    handGoalPos =
                            obstacleObject.transform.position +
                            Vector3.up * obstacleObject.transform.localScale.y / 2f -
                            Vector3.forward * obstacleObject.transform.localScale.z / 2f;
                }
                else
                {
                    handGoalPos =
                            obstacleObject.transform.position +
                            Vector3.up * obstacleObject.transform.localScale.y / 2f +
                            Vector3.forward * obstacleObject.transform.localScale.z / 2f;
                }

                currentClip = clipInfos[index];
                doingIK = true;
                yield return new WaitForSeconds(clipInfos[index].clip.length);
                doingIK = false;
            }
            else
            {
                yield return new WaitForSeconds(clipInfos[index].clip.length);
            }

            for (int ii = 0; ii < clipInfos[index].preExitClips.Length; ii++)
            {
                animator.Play(clipInfos[index].preExitClips[ii].name);
                yield return new WaitForSeconds(clipInfos[index].preExitClips[ii].length);
            }

            if (clipInfos[index].exitClip != null)
            {
                if (clipInfos[index].HasPlatform())
                {
                    StartCoroutine(MakeObjectDisappear(platformObject, clipInfos[index].exitClip.length - 0.1f, true));
                }
                if (clipInfos[index].HasObstacle())
                {
                    if (clipInfos[index].disappearObstacleImmediatelyAfterClip)
                    {
                        StartCoroutine(MakeObjectDisappear(obstacleObject, 0.05f, !clipInfos[index].HasPlatform(), 0f));
                    }
                    else
                    {
                        float len = clipInfos[index].exitClip.length;
                        float delay = 0.35f;
                        if (len <= 0.35f)
                        {
                            delay = 0f;
                        }
                        else
                        {
                            delay = len - 0.35f;
                        }
                        StartCoroutine(MakeObjectDisappear(obstacleObject, 0.35f, !clipInfos[index].HasPlatform(), delay));
                    }
                }

                Debug.LogFormat("PLAYING2: {0} dur {1}", clipInfos[index].exitClip.name, clipInfos[index].exitClip.length);
                animator.Play(clipInfos[index].exitClip.name);
                yield return new WaitForSeconds(clipInfos[index].exitClip.length);
            }
            transform.position = new Vector3(0f, 0f, transform.position.z);
            transform.rotation = Quaternion.LookRotation(transform.forward);
            if (playIdleOnComplete)
            {
                if (crossfade)
                {
                    Debug.Log("crossfade");
                    animator.CrossFadeInFixedTime(stateToPlayOnStop, 0.25f); //"Idle");
                }
                else
                {
                    Debug.Log("Play, not crossfade");
                    animator.Play(stateToPlayOnStop); //"Idle");
                }
            }
            yield return 0;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!doingIK)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                return;
            }

            float distToGoal = Mathf.Abs(goalZ - transform.position.z);
            Debug.LogFormat("Doing IK goalZ( {0} ), dist( {1} )", goalZ, distToGoal);
            if (distToGoal < 1f)
            {
                Debug.LogFormat("Setting IK: {0}", 1 - distToGoal);
                float wgt = Mathf.Clamp01(1 - distToGoal);
                if (distToGoal < currentClip.fullIKThreshDist)
                {
                    wgt = 1f;
                }
                if (currentClip.doLeftHandIK)
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, wgt);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, handGoalPos + currentClip.leftHandIKOffset);
                }
                if (currentClip.doRightHandIK)
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, wgt);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, handGoalPos + currentClip.rightHandIKOffset);
                }

            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            }
        }
    }
}