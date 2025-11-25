using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Authoring - Enemy avoidance + player separation
/// </summary>
/// 
namespace DotsNPC.Authoring
{

    public class EnemyAvoidanceAuthoring : MonoBehaviour
    {
        [Header("Enemy-Enemy Separation")]
        [Range(0.5f, 15f)]
        public float DetectionRadius = 3f;

        [Range(0f, 360f)]
        public float MaxAngleDegrees = 230f;

        [Range(0f, 1f)]
        public float AvoidanceStrength = 0.8f;

        [Header("Player Distance & Separation")]
        [Range(1f, 20f)]
        public float DesiredDistanceFromPlayer = 8f;

        [Range(0.5f, 15f)]
        public float PlayerSeparationRadius = 2.5f;

        [Header("Physical Properties")]
        [Range(0.1f, 2f)]
        public float EntityRadius = 0.5f;

        class Baker : Baker<EnemyAvoidanceAuthoring>
        {
            public override void Bake(EnemyAvoidanceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnemyAvoidance
                {
                    DetectionRadius = authoring.DetectionRadius,
                    MaxAngle = math.radians(authoring.MaxAngleDegrees),
                    AvoidanceStrength = authoring.AvoidanceStrength,
                    DesiredDistanceFromPlayer = authoring.DesiredDistanceFromPlayer,
                    PlayerSeparationRadius = authoring.PlayerSeparationRadius,
                    EntityRadius = authoring.EntityRadius
                });
            }
        }
    }
}