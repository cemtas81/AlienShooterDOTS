using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNPC.Avoidance
{
    /// <summary>
    /// Basit ama etkili avoidance sistemi
    /// - Enemy-to-Enemy collision prevention
    /// - Player separation
    /// - Doðal, titremesiz hareket
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemyAvoidanceSystem : ISystem
    {
        private EntityQuery m_EnemyQuery;
        private NativeArray<float3> m_Positions;
        private NativeArray<float> m_Radii;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EnemyQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyTag, LocalTransform, EnemyAvoidance, EnemyCollider>()
                .Build(ref state);

            state.RequireForUpdate(m_EnemyQuery);
            state.RequireForUpdate<PlayerTag>();

            m_Positions = new NativeArray<float3>(256, Allocator.Persistent);
            m_Radii = new NativeArray<float>(256, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (m_Positions.IsCreated) m_Positions.Dispose();
            if (m_Radii.IsCreated) m_Radii.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Player pozisyonunu al
            float3 playerPos = float3.zero;
            bool hasPlayer = false;

            foreach (var (tr, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
            {
                playerPos = tr.ValueRO.Position;
                hasPlayer = true;
                break;
            }

            if (!hasPlayer) return;

            int count = m_EnemyQuery.CalculateEntityCount();
            if (count == 0) return;

            // Cache boyutunu ayarla
            if (m_Positions.Length < count)
            {
                m_Positions.Dispose();
                m_Radii.Dispose();
                m_Positions = new NativeArray<float3>(count + 32, Allocator.Persistent);
                m_Radii = new NativeArray<float>(count + 32, Allocator.Persistent);
            }

            // Pozisyon ve radiuslarý cache'le (tek kere!)
            using (var transforms = m_EnemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob))
            using (var avoidances = m_EnemyQuery.ToComponentDataArray<EnemyAvoidance>(Allocator.TempJob))
            {
                for (int i = 0; i < count; i++)
                {
                    m_Positions[i] = transforms[i].Position;
                    m_Radii[i] = avoidances[i].EntityRadius;
                }
            }

            // Avoidance job'u - SADECE BU SYSTEM
            var job = new AvoidanceJob
            {
                Positions = m_Positions,
                Radii = m_Radii,
                EnemyCount = count,
                PlayerPos = playerPos,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(EnemyTag), typeof(EnemyCollider))]
        partial struct AvoidanceJob : IJobEntity
        {
            [ReadOnly] public NativeArray<float3> Positions;
            [ReadOnly] public NativeArray<float> Radii;
            public int EnemyCount;
            public float3 PlayerPos;
            public float DeltaTime;

            public void Execute(
                ref LocalTransform transform,
                in EnemyAvoidance avoidance,
                in EnemyMoveSpeed speed)
            {
                float3 pos = transform.Position;
                float myRadius = avoidance.EntityRadius;
                float detectionRadius = avoidance.DetectionRadius;
                float moveSpeed = speed.Value;

                float3 totalSeparation = float3.zero;
                float totalForce = 0f;

                // === Enemy-to-Enemy Separation ===
                for (int i = 0; i < EnemyCount; i++)
                {
                    float3 otherPos = Positions[i];
                    float otherRadius = Radii[i];

                    float3 delta = pos - otherPos;
                    float distSq = math.lengthsq(delta);

                    // Kendimize karþý kontrol etme
                    if (distSq < 0.0001f) continue;

                    // Detection radius dýþýnda ignore
                    float detectionSq = detectionRadius * detectionRadius;
                    if (distSq > detectionSq) continue;

                    float dist = math.sqrt(distSq);
                    float minDist = myRadius + otherRadius;

                    float3 direction = delta / dist;

                    // Çarpýþma var mý?
                    if (dist < minDist)
                    {
                        // Overlap: penetration'a göre push
                        float penetration = minDist - dist;
                        totalSeparation += direction * penetration * 2f;
                        totalForce = math.max(totalForce, penetration);
                    }
                    else
                    {
                        // Proximity-based gentle push
                        float proximity = 1f - (dist / detectionRadius);
                        if (proximity > 0f)
                        {
                            totalSeparation += direction * proximity * avoidance.AvoidanceStrength * 0.5f;
                        }
                    }
                }

                // === Player Separation ===
                float3 playerDelta = pos - PlayerPos;
                float playerDistSq = math.lengthsq(playerDelta);
                float playerSepRadiusSq = avoidance.PlayerSeparationRadius * avoidance.PlayerSeparationRadius;

                if (playerDistSq > 0.0001f && playerDistSq < playerSepRadiusSq)
                {
                    float playerDist = math.sqrt(playerDistSq);
                    float3 playerDir = playerDelta / playerDist;
                    float playerMinDist = myRadius + 0.4f;

                    if (playerDist < playerMinDist)
                    {
                        float penetration = playerMinDist - playerDist;
                        totalSeparation += playerDir * penetration * 3f;
                        totalForce = math.max(totalForce, penetration);
                    }
                    else
                    {
                        float proximity = 1f - (playerDist / avoidance.PlayerSeparationRadius);
                        if (proximity > 0f)
                        {
                            totalSeparation += playerDir * proximity * avoidance.AvoidanceStrength * 1.5f;
                        }
                    }
                }

                // === Apply Movement ===
                if (math.lengthsq(totalSeparation) > 0.0001f)
                {
                    totalSeparation = math.normalize(totalSeparation);

                    // Force'a göre hýz ayarla
                    float speedMultiplier = 1f;
                    if (totalForce > 1f)
                        speedMultiplier = 1.5f; // Çarpýþýrsa hýzlý kaç
                    else if (totalForce > 0.3f)
                        speedMultiplier = 0.8f; // Yakýnsa yavaþla

                    transform.Position += totalSeparation * moveSpeed * speedMultiplier * DeltaTime;
                    transform.Rotation = quaternion.LookRotationSafe(totalSeparation, new float3(0, 1, 0));
                }
            }
        }
    }
}