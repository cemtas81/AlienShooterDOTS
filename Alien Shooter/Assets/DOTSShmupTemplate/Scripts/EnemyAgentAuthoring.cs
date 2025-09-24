using UnityEngine;
using Unity.Entities;

public class EnemyAgentAuthoring : MonoBehaviour
{
    class Baker : Baker<EnemyAgentAuthoring>
    {
        public override void Bake(EnemyAgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyTag>(entity);
            //AddComponent(entity, new IsNearPlayer { Value = false }); // varsayýlan false
        }
    }
}
