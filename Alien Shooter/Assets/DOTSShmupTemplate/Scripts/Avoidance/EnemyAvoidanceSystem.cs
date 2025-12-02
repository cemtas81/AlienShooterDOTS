using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNPC.Avoidance
{
    /// <summary>
    /// Basit ve önceki doðru çalýþan avoidance mantýðý:
    /// - Enemy-to-Enemy çarpýþma önleme
    /// - Player'a fazla yaklaþýnca itme
    /// - EK: Authoring'ten verilen DesiredDistanceFromPlayer içine girince hafif dýþa itme (durma mesafesi)
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
            // Player pozisyonu
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

            // Cache boyutunu büyüt
            if (m_Positions.Length < count)
            {
                m_Positions.Dispose();
                m_Radii.Dispose();
                m_Positions = new NativeArray<float3>(count + 32, Allocator.Persistent);
                m_Radii = new NativeArray<float>(count + 32, Allocator.Persistent);
            }

            // Pozisyon + radius cache (tek pass)
            using (var transforms = m_EnemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob))
            using (var avoidances = m_EnemyQuery.ToComponentDataArray<EnemyAvoidance>(Allocator.TempJob))
            {
                for (int i = 0; i < count; i++)
                {
                    m_Positions[i] = transforms[i].Position;
                    m_Radii[i] = avoidances[i].EntityRadius;
                }
            }

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

                // === Enemy-to-Enemy Separation (ORÝJÝNAL MANTIK) ===
                for (int i = 0; i < EnemyCount; i++)
                {
                    float3 otherPos = Positions[i];
                    float otherRadius = Radii[i];
                    float3 delta = pos - otherPos;
                    float distSq = math.lengthsq(delta);
                    if (distSq < 0.0001f) continue;

                    float detectionSq = detectionRadius * detectionRadius;
                    if (distSq > detectionSq) continue;

                    float dist = math.sqrt(distSq);
                    float minDist = myRadius + otherRadius;
                    float3 direction = delta / dist;

                    if (dist < minDist)
                    {
                        float penetration = minDist - dist;
                        totalSeparation += direction * penetration * 2f;
                        totalForce = math.max(totalForce, penetration);
                    }
                    else
                    {
                        float proximity = 1f - (dist / detectionRadius);
                        if (proximity > 0f)
                            totalSeparation += direction * proximity * avoidance.AvoidanceStrength * 0.5f;
                    }
                }

                // === Player Separation + DURMA MESAFESÝ ===
                // Amaç: PlayerSeparationRadius içinde agresif itme (eski davranýþ),
                // ayrýca DesiredDistanceFromPlayer (durma mesafesi) içine girince
                // yumuþak dýþa itme ekleyerek o mesafede "durmuþ" gibi kalmasýný saðlamak.
                float3 playerDelta = pos - PlayerPos;
                float playerDistSq = math.lengthsq(playerDelta);

                if (playerDistSq > 0.0001f)
                {
                    float playerDist = math.sqrt(playerDistSq);
                    float3 playerDir = playerDelta / playerDist; // oyuncudan dýþa doðru
                    float strongSepRadius = avoidance.PlayerSeparationRadius;
                    float desiredStopDist = avoidance.DesiredDistanceFromPlayer;

                    // 1) Çok yakýnda -> güçlü itiþ (ESKÝ KOD)
                    if (playerDist < strongSepRadius)
                    {
                        float playerMinDist = myRadius + 0.4f;
                        if (playerDist < playerMinDist)
                        {
                            float penetration = playerMinDist - playerDist;
                            totalSeparation += playerDir * penetration * 3f;
                            totalForce = math.max(totalForce, penetration);
                        }
                        else
                        {
                            float proximity = 1f - (playerDist / strongSepRadius);
                            if (proximity > 0f)
                                totalSeparation += playerDir * proximity * avoidance.AvoidanceStrength * 1.5f;
                        }
                    }
                    // 2) Durma mesafesinin içinde ama güçlü separation bölgesinin dýþýnda
                    // PlayerDist < desiredStopDist -> hafif dýþa it: orbit / durma halkasýný koru
                    else if (playerDist < desiredStopDist)
                    {
                        float proximity = 1f - (playerDist / desiredStopDist); // 0 uzak, 1 çok yakýn
                        if (proximity > 0f)
                        {
                            // Daha yumuþak çarpan (1f) kullanýyoruz, istersen artýrabilirsin
                            totalSeparation += playerDir * proximity * avoidance.AvoidanceStrength;
                        }
                    }
                    // 3) desiredStopDist'ten uzaksa hiçbir içe çekme eklemiyoruz (orijinal davranýþ korunur)
                }

                // === Hareket Uygulama (ORÝJÝNAL) ===
                if (math.lengthsq(totalSeparation) > 0.0001f)
                {
                    totalSeparation = math.normalize(totalSeparation);

                    float speedMultiplier = 1f;
                    if (totalForce > 1f)
                        speedMultiplier = 1.5f;
                    else if (totalForce > 0.3f)
                        speedMultiplier = 0.8f;

                    transform.Position += totalSeparation * moveSpeed * speedMultiplier * DeltaTime;
                    transform.Rotation = quaternion.LookRotationSafe(totalSeparation, new float3(0, 1, 0));
                }
            }
        }
    }
}