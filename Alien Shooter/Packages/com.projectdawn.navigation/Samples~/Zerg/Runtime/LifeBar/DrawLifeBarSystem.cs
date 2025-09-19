using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation.Sample.Zerg
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DrawLifeBarSystem : SystemBase
    {
        LifeBar m_LifeBar;
        protected override void OnCreate()
        {
            m_LifeBar = GameObject.FindObjectOfType<LifeBar>(true);
        }

        protected override void OnUpdate()
        {
            if (m_LifeBar == null)
                return;

            m_LifeBar.UpdateProperties();

            foreach (var (life, shape, transform) in Query< UnitLife, AgentShape, LocalTransform>())
            {
                m_LifeBar.Draw(transform.Position, life.Life / life.MaxLife, (int)(shape.Radius / 0.2f), shape.Radius, shape.Height);
            }
        }
    }
}
