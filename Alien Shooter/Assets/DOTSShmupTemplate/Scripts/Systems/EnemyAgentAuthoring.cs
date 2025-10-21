using AnimCooker;
using ProjectDawn.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemyAgentAuthoring : MonoBehaviour
{
    public float AttackRange = 6f; 
    public int Damage = 10;
    public Transform firePosition; // float3 yerine Transform kullan

    class Baker : Baker<EnemyAgentAuthoring>
    {
        public override void Bake(EnemyAgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Transform'un local pozisyonunu float3'e çevir
            float3 localFirePos = float3.zero;
            if (authoring.firePosition != null)
            {
                // Transform'un local pozisyonunu al
                Vector3 localPos = authoring.firePosition.localPosition;
                localFirePos = new float3(localPos.x, localPos.y, localPos.z);
            }

            AddComponent(entity, new BulletData { firePos = localFirePos });
            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new AttackRange { Value = authoring.AttackRange });
            AddComponent(entity, new DamageComponent { Value = authoring.Damage });
            AddBuffer<AttackFlag>(entity);
            AddComponent(entity, new Cooldown { Value = 0f });
            //AddComponent(entity, new AnimationStateData
            //{
            //    ModelIndex = 0, // Model index in the database
            //    ForeverClipIndex = 0 // Default animation index
            //});
        }
    }
}
