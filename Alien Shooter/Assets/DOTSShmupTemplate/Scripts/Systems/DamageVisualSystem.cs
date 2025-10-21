using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemyDamageSystem))]
public partial struct DamageVisualSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DamageVisualComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Hasar görsel efekti olan tüm düþmanlarý iþle
        foreach (var (damageVisual, entity) in
                 SystemAPI.Query<RefRW<DamageVisualComponent>>()
                          .WithEntityAccess())
        {
            // Süreyi güncelle
            damageVisual.ValueRW.CurrentTime += deltaTime;

            // Süre dolduysa normal renge geri döndür
            if (damageVisual.ValueRW.CurrentTime >= damageVisual.ValueRO.Duration)
            {
                // Orijinal rengi geri yükle
                if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(entity))
                {
                    var originalColor = damageVisual.ValueRO.OriginalColor;
                    ecb.SetComponent(entity, new URPMaterialPropertyBaseColor
                    {
                        Value = new float4(originalColor.x, originalColor.y, originalColor.z, 1)
                    });
                }

                // Hasar görselleþtirme bileþenini kaldýr
                ecb.RemoveComponent<DamageVisualComponent>(entity);
            }
            // Süre dolmadýysa, flash efekti için titreþimli renk deðiþimi yapabilirsiniz (opsiyonel)
            else if (damageVisual.ValueRO.CurrentTime < damageVisual.ValueRO.Duration / 2)
            {
                // Ýlk yarýda parlak kýrmýzý
                ecb.SetComponent(entity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(1, 0.3f, 0.3f, 1)
                });
            }
            else
            {
                // Ýkinci yarýda daha az parlak kýrmýzý (beyaza doðru geçiþ)
                float ratio = (damageVisual.ValueRO.CurrentTime - damageVisual.ValueRO.Duration / 2) / (damageVisual.ValueRO.Duration / 2);
                float3 originalColor = damageVisual.ValueRO.OriginalColor;
                float3 damageColor = new float3(1, 0.3f, 0.3f);
                float3 currentColor = math.lerp(damageColor, originalColor, ratio);

                ecb.SetComponent(entity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(currentColor.x, currentColor.y, currentColor.z, 1)
                });
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}