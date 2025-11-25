using Unity.Entities;
using UnityEngine;

namespace DotsNPC.Authoring
{
    public class EnemyAuthoring : MonoBehaviour
    {
        public float MoveSpeed = 3f;
        public int Damage = 10;
        public float AttackRange = 6f;

    }

    public class EnemyBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new EnemyMoveSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new DamageComponent { Value = authoring.Damage });
            AddComponent(entity, new AttackRange { Value = authoring.AttackRange });
            AddBuffer<AttackFlag>(entity);
            AddComponent(entity, new Cooldown { Value = 0f });
        }
    }
}