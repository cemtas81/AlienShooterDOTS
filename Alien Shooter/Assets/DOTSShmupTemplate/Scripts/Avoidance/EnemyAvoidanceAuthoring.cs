using Unity.Entities;
using UnityEngine;
using DotsNPC.Avoidance;

public class EnemyAvoidanceAuthoring : MonoBehaviour
{
    [Header("Avoidance Settings")]
    public float DetectionRadius = 3f;
    public float AvoidanceStrength = 1f;
    public float PlayerSeparationRadius = 2f;
    public float EntityRadius = 0.5f;

    class EnemyAvoidanceBaker : Baker<EnemyAvoidanceAuthoring>
    {
        public override void Bake(EnemyAvoidanceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyAvoidance
            {
                DetectionRadius = authoring.DetectionRadius,
                AvoidanceStrength = authoring.AvoidanceStrength,
                PlayerSeparationRadius = authoring.PlayerSeparationRadius,
                EntityRadius = authoring.EntityRadius,
                DesiredDistanceFromPlayer = 5f,
                MaxAngle = 0f // Kullanýlmýyor
            });

            AddComponent(entity, new EnemyCollider());
        }
    }
}