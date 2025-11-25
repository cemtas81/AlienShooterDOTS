using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Threepeat {
    public class ForceZeroXPostition : MonoBehaviour
    {

        private void LateUpdate()
        {
            if (transform.position.x != 0)
            {
                transform.position = new Vector3(0, transform.position.y, transform.position.z);
            }
        }
    }
}