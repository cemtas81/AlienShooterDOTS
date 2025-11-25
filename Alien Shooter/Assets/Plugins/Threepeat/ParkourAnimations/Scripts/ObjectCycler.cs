using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Threepeat
{
    public class ObjectCycler : MonoBehaviour
    {
        public GameObject[] objects;

        public int currIndex = 0;

        private float lastChangeTime = 0f;
        public float changeInterval = 8f;

        public KeyCode resetHotkey = KeyCode.R;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (objects.Length == 0)
            {
                return;
            }
            if (Input.GetKeyDown(resetHotkey))
            {
                lastChangeTime = Time.time;
                objects[currIndex].SetActive(false);
                currIndex = 0;
                objects[currIndex].SetActive(true);
                return;
            }

            if ((Time.time - lastChangeTime) > changeInterval)
            {
                objects[currIndex].SetActive(false);
                currIndex++;
                if (currIndex >= objects.Length)
                {
                    currIndex = 0;
                }
                objects[currIndex].SetActive(true);
                lastChangeTime = Time.time;
            }
        }
    }
}