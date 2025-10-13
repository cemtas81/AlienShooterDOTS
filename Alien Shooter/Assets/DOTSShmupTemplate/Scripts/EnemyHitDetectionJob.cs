using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public struct EnemyHitDetectionJob : IJob
{
    [ReadOnly] public NativeArray<LocalTransform> EnemyTransforms;
    [ReadOnly] public float3 RayOrigin;
    [ReadOnly] public float3 RayDirection;
    [ReadOnly] public float HitRadius;

    public NativeReference<EnemyHitResult> Result;

    public void Execute()
    {
        var result = new EnemyHitResult
        {
            HitEnemy = false,
            TargetPosition = float3.zero,
            NearestDistance = float.MaxValue
        };

        for (int i = 0; i < EnemyTransforms.Length; i++)
        {
            float3 enemyPos = EnemyTransforms[i].Position;
            float3 toEnemy = enemyPos - RayOrigin;
            float dot = math.dot(math.normalize(toEnemy), RayDirection);

            if (dot > 0)
            {
                float distanceToRay = math.length(math.cross(toEnemy, RayDirection));
                float distanceToEnemy = math.length(toEnemy);

                if (distanceToRay < HitRadius && distanceToEnemy < result.NearestDistance)
                {
                    result.NearestDistance = distanceToEnemy;
                    result.TargetPosition = enemyPos;
                    result.HitEnemy = true;
                }
            }
        }

        Result.Value = result;
    }
}

public struct EnemyHitResult
{
    public bool HitEnemy;
    public float3 TargetPosition;
    public float NearestDistance;
}