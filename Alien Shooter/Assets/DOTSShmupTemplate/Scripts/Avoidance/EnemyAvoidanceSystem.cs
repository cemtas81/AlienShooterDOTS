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
                LateralThreshold = 0.8f,
                TurnSpeedDegPerSec = 180f,
                BlendSeparationFactor = 0.8f, // Artýrýldý
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
            public float LateralThreshold;
            public float TurnSpeedDegPerSec;
            public float BlendSeparationFactor;
            public float RadiusTolerance;
            public float MinStoppingDistance;

            public void Execute(ref LocalTransform transform, in EnemyAvoidance avoidance, in EnemyMoveSpeed moveSpeed)
            {
                float3 currentPos = transform.Position;
                float3 currentForward = transform.Forward();

                float3 toPlayer = PlayerPos - currentPos;
                float distToPlayer = math.length(toPlayer);
                float3 dirToPlayer = distToPlayer > 0.0001f ? toPlayer / distToPlayer : new float3(0, 0, 1);

                float desiredRadius = avoidance.DesiredDistanceFromPlayer;
                float radialError = distToPlayer - desiredRadius;

                // Separation her zaman hesapla (overlap engellemek için)
                float3 separation = ComputeSeparationOptimized(currentPos, avoidance);
                float separationMagnitude = math.length(separation);

                // ===== EMERGENCY SEPARATION (Çakýþma varsa öncelik ver) =====
                if (separationMagnitude > 2f) // Güçlü separation gerekli
                {
                    float3 separationDir = SafeNormalize(separation);
                    transform.Position += separationDir * moveSpeed.Value * 1.5f * DeltaTime; // Hýzlý uzaklaþ
                    transform.Rotation = quaternion.LookRotationSafe(separationDir, new float3(0, 1, 0));
                    return;
                }

                // ===== STOPPING ZONE (Tolerans içinde ise dur) =====
                if (math.abs(radialError) <= RadiusTolerance)
                {
                    float3 upDir = new float3(0, 1, 0);
                    if (separationMagnitude > 0.0001f)
                    {
                        float3 separationDir = SafeNormalize(separation);
                        transform.Rotation = quaternion.LookRotationSafe(separationDir, upDir);

                        // Separation varsa hafif hareket et
                        if (separationMagnitude > 0.5f)
                        {
                            transform.Position += separationDir * moveSpeed.Value * 0.5f * DeltaTime;
                        }
                    }
                    return;
                }

                // ===== APPROACH/RETREAT PHASE =====
                float needRadialAdjust = math.sign(radialError);

                float3 upDir2 = new float3(0, 1, 0);
                float3 tangent = math.normalize(math.cross(upDir2, dirToPlayer));

                // Radyal ayarlama ile yaklaþ/uzaklaþ
                float3 approachDir = math.normalize(
                    dirToPlayer * needRadialAdjust * 0.7f +
                    tangent * 0.3f
                );

                bool blocked = IsForwardBlockedOptimized(currentPos, currentForward, avoidance);

                float3 chosenDir = blocked
                    ? SampleBestSectorOptimized(currentPos, avoidance)
                    : approachDir;

                // Separation'ý blend et (güçlendirildi)
                if (separationMagnitude > 0.0001f)
                {
                    float3 separationDir = SafeNormalize(separation);
                    float blendFactor = math.min(BlendSeparationFactor + separationMagnitude * 0.2f, 1f);
                    chosenDir = math.normalize(chosenDir * (1f - blendFactor) + separationDir * blendFactor);
                }

                float3 finalDir = RotateTowards(currentForward, chosenDir, math.radians(TurnSpeedDegPerSec) * DeltaTime);

                float forwardScale = blocked ? 0.2f : 1f;

                // Separation kuvvetli ise yavaþla
                if (separationMagnitude > 1f)
                    forwardScale *= 0.5f;

                transform.Position += finalDir * moveSpeed.Value * forwardScale * DeltaTime;
                transform.Rotation = quaternion.LookRotationSafe(finalDir, upDir2);
            }

            float3 ComputeSeparationOptimized(float3 pos, EnemyAvoidance avoidance)
            {
                float3 accum = 0;
                float weightSum = 0f;

                float rEnemy = avoidance.DetectionRadius;
                float rEnemySq = rEnemy * rEnemy;
                float entityRadius = avoidance.EntityRadius;

                // Enemy-Enemy separation (güçlendirildi)
                for (int i = 0; i < PositionCount; i++)
                {
                    float3 other = Positions[i];
                    float3 diff = pos - other;
                    float distSq = math.lengthsq(diff);
                    if (distSq < 0.0001f || distSq > rEnemySq)
                        continue;

                    float dist = math.sqrt(distSq);
                    float minDistance = entityRadius * 2f; // Çakýþma mesafesi

                    // Çakýþma durumunda çok güçlü force
                    float force;
                    if (dist < minDistance)
                    {
                        force = 10f; // Çok güçlü iterilme
                    }
                    else
                    {
                        float proximity = 1f - (dist - minDistance) / (rEnemy - minDistance);
                        proximity = math.max(0f, proximity);
                        force = proximity * avoidance.AvoidanceStrength;
                    }

                    float3 dirNorm = diff * math.rsqrt(distSq);
                    accum += dirNorm * force;
                    weightSum += force;
                }

                // Player separation (güçlendirildi)
                float rPlayer = avoidance.PlayerSeparationRadius;
                float3 diffP = pos - PlayerPos;
                float distSqP = math.lengthsq(diffP);
                float rPlayerSq = rPlayer * rPlayer;

                if (distSqP > 0.0001f && distSqP < rPlayerSq)
                {
                    float distP = math.sqrt(distSqP);
                    float minPlayerDistance = 1f; // Player için minimum mesafe

                    float playerForce;
                    if (distP < minPlayerDistance)
                    {
                        playerForce = 15f; // Player'a çok yakýnsa çok güçlü iterilme
                    }
                    else
                    {
                        float proximityP = 1f - (distP - minPlayerDistance) / (rPlayer - minPlayerDistance);
                        proximityP = math.max(0f, proximityP);
                        playerForce = proximityP * avoidance.AvoidanceStrength * 3f;
                    }

                    float3 dirNormP = diffP * math.rsqrt(distSqP);
                    accum += dirNormP * playerForce;
                    weightSum += playerForce;
                }

                if (weightSum <= 0f)
                    return 0;

                float3 sep = accum / weightSum;

                // Maksimum force limiti artýrýldý
                float maxForce = 15f * avoidance.AvoidanceStrength;
                float len = math.length(sep);
                if (len > maxForce)
                    sep *= (maxForce / len);

                return sep;
            }

            bool IsForwardBlockedOptimized(float3 pos, float3 forwardDir, EnemyAvoidance avoidance)
            {
                float blockDist = math.min(1.0f, avoidance.DetectionRadius);
                float blockDistSq = blockDist * blockDist;
                float entityRadius = avoidance.EntityRadius;

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

                    // Entity radius'u dikkate al
                    if (latMag < LateralThreshold + entityRadius)
                        return true;
                }

                // Player ile de kontrol et
                float3 toPlayer = PlayerPos - pos;
                float playerDistSq = math.lengthsq(toPlayer);
                if (playerDistSq < blockDistSq)
                {
                    float3 playerDirNorm = toPlayer * math.rsqrt(playerDistSq);
                    float playerForwardDot = math.dot(forwardDir, playerDirNorm);
                    if (playerForwardDot > 0f)
                    {
                        float playerProjLen = math.dot(toPlayer, forwardDir);
                        float3 playerLateral = toPlayer - forwardDir * playerProjLen;
                        float playerLatMag = math.length(playerLateral);

                        if (playerLatMag < LateralThreshold + 1f) // Player için daha geniþ alan
                            return true;
                    }
                }

                return false;
            }

            float3 SampleBestSectorOptimized(float3 pos, EnemyAvoidance avoidance)
            {
                int count = math.max(8, SectorCount); // Daha fazla sektör
                float bestScore = -1f;
                float3 bestDir = new float3(0, 0, 0);

                for (int s = 0; s < count; s++)
                {
                    float angle = (2f * math.PI * s) / count;
                    float3 dir = new float3(math.sin(angle), 0f, math.cos(angle));
                    float score = ProbeDirectionOptimized(pos, dir, avoidance);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDir = dir;
                    }
                }
                return (bestScore < 0f) ? new float3(0, 0, 1) : bestDir;
            }

            float ProbeDirectionOptimized(float3 pos, float3 dir, EnemyAvoidance avoidance)
            {
                float maxDist = avoidance.DetectionRadius;
                float closest = maxDist;
                float maxDistSq = maxDist * maxDist;
                float entityRadius = avoidance.EntityRadius;

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

                    // Entity radius'u dikkate al
                    float checkRadius = entityRadius * 2f;
                    if (latMagSq < checkRadius * checkRadius)
                    {
                        float dist = math.sqrt(distSq);
                        if (dist < closest)
                            closest = dist;
                    }
                }

                // Player'ý da kontrol et
                float3 playerV = PlayerPos - pos;
                float playerDistSq = math.lengthsq(playerV);
                if (playerDistSq <= maxDistSq)
                {
                    float playerProjection = math.dot(playerV, dir);
                    if (playerProjection > 0f)
                    {
                        float3 playerLateral = playerV - dir * playerProjection;
                        float playerLatMagSq = math.lengthsq(playerLateral);

                        if (playerLatMagSq < 4f) // Player için 2 birim radius
                        {
                            float playerDist = math.sqrt(playerDistSq);
                            if (playerDist < closest)
                                closest = playerDist;
                        }
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