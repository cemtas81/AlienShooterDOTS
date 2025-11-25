using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNPC.Avoidance
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemyAvoidanceSystem : ISystem
    {
        private EntityQuery m_PositionsQuery;
        private NativeArray<float3> m_CachedPositions;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_PositionsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyTag, LocalTransform, EnemyMoveSpeed>()
                .Build(ref state);

            state.RequireForUpdate(m_PositionsQuery);
            state.RequireForUpdate<PlayerTag>();

            m_CachedPositions = new NativeArray<float3>(256, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (m_CachedPositions.IsCreated)
                m_CachedPositions.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            float3 playerPos = float3.zero;
            bool hasPlayer = false;
            foreach (var (plTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
            {
                playerPos = plTransform.ValueRO.Position;
                hasPlayer = true;
                break;
            }
            if (!hasPlayer)
                return;

            int enemyCount = m_PositionsQuery.CalculateEntityCount();
            if (enemyCount == 0)
                return;

            if (m_CachedPositions.Length < enemyCount)
            {
                m_CachedPositions.Dispose();
                m_CachedPositions = new NativeArray<float3>(enemyCount + 32, Allocator.Persistent);
            }

            using (var transforms = m_PositionsQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob))
            {
                for (int i = 0; i < transforms.Length; i++)
                    m_CachedPositions[i] = transforms[i].Position;
            }

            var job = new AdvancedAvoidanceJob
            {
                DeltaTime = deltaTime,
                PlayerPos = playerPos,
                Positions = m_CachedPositions,
                PositionCount = enemyCount,
                SectorCount = 12,
                BlockDistance = 1.0f,
                LateralThreshold = 0.8f,
                SeparationMaxForce = 5f,
                TurnSpeedDegPerSec = 180f,
                BlendSeparationFactor = 0.6f,
                DesiredRadius = 8f,
                RadiusTolerance = 1.5f,
                MinStoppingDistance = 0.3f
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(EnemyTag))]
        partial struct AdvancedAvoidanceJob : IJobEntity
        {
            [ReadOnly] public NativeArray<float3> Positions;
            public int PositionCount;

            public float3 PlayerPos;
            public float DeltaTime;

            public int SectorCount;
            public float BlockDistance;
            public float LateralThreshold;
            public float SeparationMaxForce;
            public float TurnSpeedDegPerSec;
            public float BlendSeparationFactor;
            public float DesiredRadius;
            public float RadiusTolerance;
            public float MinStoppingDistance;

            public void Execute(ref LocalTransform transform, in EnemyAvoidance avoidance, in EnemyMoveSpeed moveSpeed)
            {
                float3 currentPos = transform.Position;
                float3 currentForward = transform.Forward();

                float3 toPlayer = PlayerPos - currentPos;
                float distToPlayer = math.length(toPlayer);
                float3 dirToPlayer = distToPlayer > 0.0001f ? toPlayer / distToPlayer : new float3(0, 0, 1);

                float radialError = distToPlayer - DesiredRadius;

                // ===== STOPPING ZONE (Tolerans içinde ise dur) =====
                if (math.abs(radialError) <= RadiusTolerance)
                {
                    // Sadece separation (diðer düþmanlardan uzak dur)
                    float3 separation = ComputeSeparationOptimized(currentPos, avoidance);

                    float3 upDir = new float3(0, 1, 0);
                    if (math.lengthsq(separation) > 0.0001f)
                    {
                        float3 separationDir = SafeNormalize(separation);
                        transform.Rotation = quaternion.LookRotationSafe(separationDir, upDir);

                        // Sadece çakýþma varsa hafif hareket et
                        float sepLen = math.length(separation);
                        if (sepLen > 1f)
                        {
                            transform.Position += separationDir * moveSpeed.Value * 0.3f * DeltaTime;
                        }
                    }
                    return;
                }

                // ===== APPROACH/RETREAT PHASE =====
                float needRadialAdjust = math.sign(radialError); // -1 (çok yakýn) veya +1 (çok uzak)

                float3 upDir2 = new float3(0, 1, 0);
                float3 tangent = math.normalize(math.cross(upDir2, dirToPlayer));

                // Radyal ayarlama ile yaklaþ/uzaklaþ
                float3 approachDir = math.normalize(
                    dirToPlayer * needRadialAdjust * 0.7f +
                    tangent * 0.3f  // Hafif dairesel bileþen
                );

                float3 separation2 = ComputeSeparationOptimized(currentPos, avoidance);

                bool blocked = IsForwardBlockedOptimized(currentPos, currentForward, avoidance.DetectionRadius);

                float3 chosenDir = blocked
                    ? SampleBestSectorOptimized(currentPos, avoidance.DetectionRadius)
                    : approachDir;

                // Separation'ý blend et
                if (math.lengthsq(separation2) > 0.0001f)
                    chosenDir = math.normalize(chosenDir * (1f - BlendSeparationFactor) + separation2 * BlendSeparationFactor);

                float3 finalDir = RotateTowards(currentForward, chosenDir, math.radians(TurnSpeedDegPerSec) * DeltaTime);

                float forwardScale = blocked ? 0.2f : 1f;

                transform.Position += finalDir * moveSpeed.Value * forwardScale * DeltaTime;
                transform.Rotation = quaternion.LookRotationSafe(finalDir, upDir2);
            }

            float3 ComputeSeparationOptimized(float3 pos, EnemyAvoidance avoidance)
            {
                float3 accum = 0;
                float weightSum = 0f;

                float rEnemy = avoidance.DetectionRadius;
                float rEnemySq = rEnemy * rEnemy;
                float rPlayer = avoidance.PlayerSeparationRadius;
                float rPlayerSq = rPlayer * rPlayer;

                for (int i = 0; i < PositionCount; i++)
                {
                    float3 other = Positions[i];
                    float3 diff = pos - other;
                    float distSq = math.lengthsq(diff);
                    if (distSq < 0.0001f || distSq > rEnemySq)
                        continue;

                    float dist = math.sqrt(distSq);
                    float proximity = 1f - dist / rEnemy;
                    float3 dirNorm = diff * math.rsqrt(distSq);
                    accum += dirNorm * proximity;
                    weightSum += proximity;
                }

                float3 diffP = pos - PlayerPos;
                float distSqP = math.lengthsq(diffP);
                if (distSqP > 0.0001f && distSqP < rPlayerSq)
                {
                    float distP = math.sqrt(distSqP);
                    float proximityP = 1f - distP / rPlayer;
                    float3 dirNormP = diffP * math.rsqrt(distSqP);
                    accum += dirNormP * proximityP * 1.2f;
                    weightSum += proximityP * 1.2f;
                }

                if (weightSum <= 0f)
                    return 0;

                float3 sep = accum / weightSum;
                float len = math.length(sep);
                if (len > SeparationMaxForce)
                    sep *= (SeparationMaxForce / len);
                return sep;
            }

            bool IsForwardBlockedOptimized(float3 pos, float3 forwardDir, float detectionRadius)
            {
                float blockDist = math.min(BlockDistance, detectionRadius);
                float blockDistSq = blockDist * blockDist;
                float latThresh = LateralThreshold;

                for (int i = 0; i < PositionCount; i++)
                {
                    float3 other = Positions[i];
                    float3 toOther = other - pos;
                    float distSq = math.lengthsq(toOther);
                    if (distSq < 0.0001f || distSq > blockDistSq)
                        continue;

                    float3 dirNorm = toOther * math.rsqrt(distSq);
                    float forwardDot = math.dot(forwardDir, dirNorm);
                    if (forwardDot <= 0f)
                        continue;

                    float projLen = math.dot(toOther, forwardDir);
                    float3 lateral = toOther - forwardDir * projLen;
                    float latMag = math.length(lateral);
                    if (latMag < latThresh)
                        return true;
                }
                return false;
            }

            float3 SampleBestSectorOptimized(float3 pos, float detectionRadius)
            {
                int count = math.max(3, SectorCount);
                float bestScore = -1f;
                float3 bestDir = new float3(0, 0, 0);

                for (int s = 0; s < count; s++)
                {
                    float angle = (2f * math.PI * s) / count;
                    float3 dir = new float3(math.sin(angle), 0f, math.cos(angle));
                    float score = ProbeDirectionOptimized(pos, dir, detectionRadius);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDir = dir;
                    }
                }
                return (bestScore < 0f) ? new float3(0, 0, 1) : bestDir;
            }

            float ProbeDirectionOptimized(float3 pos, float3 dir, float detectionRadius)
            {
                float maxDist = detectionRadius;
                float closest = maxDist;
                float maxDistSq = maxDist * maxDist;

                for (int i = 0; i < PositionCount; i++)
                {
                    float3 other = Positions[i];
                    float3 v = other - pos;
                    float distSq = math.lengthsq(v);
                    if (distSq > maxDistSq)
                        continue;

                    float projection = math.dot(v, dir);
                    if (projection <= 0f)
                        continue;

                    float3 lateral = v - dir * projection;
                    float latMagSq = math.lengthsq(lateral);
                    if (latMagSq < 0.25f)
                    {
                        float dist = math.sqrt(distSq);
                        if (dist < closest)
                            closest = dist;
                    }
                }
                return closest;
            }

            float3 RotateTowards(float3 currentForward, float3 targetDir, float maxTurnRadians)
            {
                currentForward = SafeNormalize(currentForward);
                targetDir = SafeNormalize(targetDir);

                float d = math.clamp(math.dot(currentForward, targetDir), -1f, 1f);
                float angle = math.acos(d);
                if (angle < 1e-4f)
                    return targetDir;

                float t = (angle <= maxTurnRadians) ? 1f : (maxTurnRadians / angle);
                float3 axis = SafeNormalize(math.cross(currentForward, targetDir));
                if (math.lengthsq(axis) < 1e-6f)
                    return targetDir;

                quaternion rot = quaternion.AxisAngle(axis, angle * t);
                float3 newDir = math.mul(rot, currentForward);
                return SafeNormalize(newDir);
            }

            static float3 SafeNormalize(float3 v)
            {
                float lenSq = math.lengthsq(v);
                return lenSq < 1e-8f ? 0 : v * math.rsqrt(lenSq);
            }
        }
    }
}