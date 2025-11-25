using UnityEngine;
// credit: this script is based on https://gist.github.com/bendux/76a9b52710b63e284ce834310f8db773

namespace Threepeat
{
    public class CameraSmoothFollow : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 0f, -10f);
        public float smoothTime = 0.25f;
        private Vector3 velocity = Vector3.zero;

        [SerializeField] private Transform target;

        private void Update()
        {
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}