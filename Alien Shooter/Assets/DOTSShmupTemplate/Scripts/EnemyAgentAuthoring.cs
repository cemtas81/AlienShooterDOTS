using UnityEngine;
using Unity.Entities;

public class EnemyAgentAuthoring : MonoBehaviour
{
    public float AttackRange = 6f; 
    public int Damage = 10;

    //public GameObject BulletPrefab;      // Sadece ranged için
    //public GameObject MeleeArmPrefab;    // Sadece melee için
    //public bool IsRanged = false;
    class Baker : Baker<EnemyAgentAuthoring>
    {
        public override void Bake(EnemyAgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new AttackRange { Value = authoring.AttackRange });
            AddComponent(entity, new DamageComponent { Value = authoring.Damage });
            AddBuffer<AttackFlag>(entity);
            AddComponent(entity, new Cooldown { Value = 0f });
            
        }
    }
}
