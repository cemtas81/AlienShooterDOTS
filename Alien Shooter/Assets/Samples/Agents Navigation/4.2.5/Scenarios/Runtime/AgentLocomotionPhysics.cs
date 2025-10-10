using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ProjectDawn.Entities;
using ProjectDawn.Navigation.Hybrid;

namespace ProjectDawn.Navigation.Sample.Scenarios
{
    [RequireComponent(typeof(AgentAuthoring))]
    [RequireComponent(typeof(Rigidbody))]
    public class AgentLocomotionPhysics : MonoBehaviour
    {
        public AgentLocomotion Locomotion = AgentLocomotion.Default;

        AgentAuthoring m_Agent;
        AgentCylinderShapeAuthoring m_CylinderShape;
        Rigidbody m_Rigidbody;

        ref AgentBody Body => ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<AgentBody>(m_Agent.GetOrCreateEntity()).ValueRW;

        void OnEnable()
        {
            m_Agent = GetComponent<AgentAuthoring>();
            m_CylinderShape = GetComponent<AgentCylinderShapeAuthoring>();
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            ref AgentBody body = ref Body;

            if (body.IsStopped)
                return;

            float3 towards = body.Destination - (float3)transform.position;
            float distance = math.length(towards);
            float3 desiredDirection = distance > math.EPSILON ? towards / distance : float3.zero;
            body.Force = desiredDirection;
            body.RemainingDistance = distance;
#if UNITY_6000_0_OR_NEWER
            body.Velocity = m_Rigidbody.linearVelocity;
#else
            body.Velocity = m_Rigidbody.velocity;
#endif
        }

        void LateUpdate()
        {
            ref AgentBody body = ref Body;
            AgentShape shape = m_CylinderShape.EntityShape;
            AgentLocomotion locomotion = Locomotion;
            float DeltaTime = Time.deltaTime;

            if (body.IsStopped)
                return;

            // Check, if we reached the destination
            float remainingDistance = body.RemainingDistance;
            if (remainingDistance <= locomotion.StoppingDistance + 1e-3f)
            {
                body.Velocity = 0;
                body.IsStopped = true;
                return;
            }

            float maxSpeed = locomotion.Speed;

            // Start breaking if close to destination
            if (locomotion.AutoBreaking)
            {
                float breakDistance = shape.Radius * 2 + locomotion.StoppingDistance;
                if (remainingDistance <= breakDistance)
                {
                    maxSpeed = math.lerp(locomotion.Speed * 0.25f, locomotion.Speed, remainingDistance / breakDistance);
                }
            }

            // Force force to be maximum of unit length, but can be less
            float forceLength = math.length(body.Force);
            if (forceLength > 1)
                body.Force = body.Force / forceLength;

            // Interpolate velocity
            body.Velocity = math.lerp(body.Velocity, body.Force * maxSpeed, math.saturate(DeltaTime * locomotion.Acceleration));

#if UNITY_6000_0_OR_NEWER
            m_Rigidbody.linearVelocity = body.Velocity;
#else
            m_Rigidbody.velocity = body.Velocity;
#endif

            float speed = math.length(body.Velocity);

            // Early out if steps is going to be very small
            if (speed < 1e-3f)
                return;

            // Avoid over-stepping the destination
            if (speed * DeltaTime > remainingDistance)
            {
                transform.position = (float3) transform.position + (body.Velocity / speed) * remainingDistance;
                return;
            }

            // Update position
            transform.position = (float3)transform.position + DeltaTime * body.Velocity;

            // Update rotation
            float angle = math.atan2(body.Velocity.x, body.Velocity.z);
            transform.rotation = math.slerp(transform.rotation, quaternion.RotateY(angle), DeltaTime * locomotion.AngularSpeed);
        }
    }
}
