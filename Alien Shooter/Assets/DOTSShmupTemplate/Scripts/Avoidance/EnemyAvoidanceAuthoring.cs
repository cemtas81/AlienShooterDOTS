using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Authoring - Enemy avoidance + player separation
/// </summary>
public class EnemyAvoidanceAuthoring : MonoBehaviour
{
    [Header("Enemy-Enemy Separation")]
    [Range(0.5f, 15f)]
    public float DetectionRadius = 3f;

    [Range(0f, 360f)]
    public float MaxAngleDegrees = 230f;

    [Range(0f, 1f)]
    public float AvoidanceStrength = 0.8f;

    [Header("Enemy-Player Separation")]
    [Range(0.5f, 15f)]
    public float PlayerSeparationRadius = 2.5f;

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
                PlayerSeparationRadius = authoring.PlayerSeparationRadius
            });
        }
    }
}