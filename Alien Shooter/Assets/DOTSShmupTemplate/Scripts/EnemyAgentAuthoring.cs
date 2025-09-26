using ProjectDawn.Navigation;
using Unity.Entities;
using UnityEngine;

public class EnemyAgentAuthoring : MonoBehaviour
{
    public float AttackRange = 6f; 
    public int Damage = 10;

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
