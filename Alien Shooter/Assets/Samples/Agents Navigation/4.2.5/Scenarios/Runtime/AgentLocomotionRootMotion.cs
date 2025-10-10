using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ProjectDawn.Entities;
using ProjectDawn.Navigation.Hybrid;

namespace ProjectDawn.Navigation.Sample.Scenarios
{
    [RequireComponent(typeof(AgentAuthoring))]
    [RequireComponent(typeof(Animator))]
    public class AgentLocomotionRootMotion : MonoBehaviour
    {
        public AgentLocomotion Locomotion = AgentLocomotion.Default;

        AgentAuthoring m_Agent;
        AgentCylinderShapeAuthoring m_CylinderShape;
        Animator m_Animator;

        ref AgentBody Body => ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<AgentBody>(m_Agent.GetOrCreateEntity()).ValueRW;

        void OnEnable()
        {
            m_Agent = GetComponent<AgentAuthoring>();
            m_CylinderShape = GetComponent<AgentCylinderShapeAuthoring>();
            m_Animator = GetComponent<Animator>();
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
            //body.Velocity = m_Animator.velocity;
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
                m_Animator.SetFloat("Horizontal Force", 0);
                m_Animator.SetFloat("Vertical Force", 0);
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

            float3 force = body.Velocity;

            force = math.rotate(math.inverse(transform.rotation), force);

            force = math.normalize(force);

            m_Animator.SetFloat("Horizontal Force", force.x);
            m_Animator.SetFloat("Vertical Force", force.z);

            float speed = math.length(body.Velocity);

            // Early out if steps is going to be very small
            if (speed < 1e-3f)
                return;

            // Avoid over-stepping the destination
            if (speed * DeltaTime > remainingDistance)
            {
                return;
            }

            // Update rotation
            float angle = math.atan2(body.Velocity.x, body.Velocity.z);
            transform.rotation = math.slerp(transform.rotation, quaternion.RotateY(angle), DeltaTime * locomotion.AngularSpeed);
        }
    }
}
