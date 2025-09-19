using Unity.Entities;
using UnityEngine;
using ProjectDawn.Entities;

namespace ProjectDawn.Navigation.Hybrid
{
    public enum AgentGroundingMode
    {
        Body,
        BodyAndForce,
    }

    /// <summary>
    /// Enables collisio with other agents.
    /// </summary>
    [RequireComponent(typeof(AgentAuthoring))]
    [AddComponentMenu("Agents Navigation/Agent Grounding")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/game-objects/grounding.html")]
    public class AgentGroundingAuthoring : MonoBehaviour
    {
        [SerializeField]
        protected LayerMask m_Layers = -1;

        [SerializeField]
        protected AgentGroundingMode m_Mode = AgentGroundingMode.BodyAndForce;

        public LayerMask Layers
        {
            get => m_Layers;
            set
            {
                if (m_Entity != Entity.Null)
                    Grounding.Layers = value;
                m_Layers = value;
            }
        }

        public AgentGroundingMode Mode
        {
            get => m_Mode;
            set
            {
                if (m_Entity != Entity.Null)
                    throw new System.InvalidOperationException("Can not modify mode once the component is initialized");
                m_Mode = value;
            }
        }

        Entity m_Entity;

        /// <summary>
        /// <see cref="AgentGrounding"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public ref AgentGrounding Grounding =>
            ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<AgentGrounding>(m_Entity).ValueRW;

        internal AgentGrounding DefaultGrounding => new()
        {
            Layers = m_Layers,
        };

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetComponent<AgentAuthoring>().GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, DefaultGrounding);
            if (m_Mode == AgentGroundingMode.BodyAndForce)
                world.EntityManager.AddComponent<AgentGroundingSlope>(m_Entity);

            // Sync in case it was created as disabled
            if (!enabled)
                world.EntityManager.SetComponentEnabled<AgentGrounding>(m_Entity, false);
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.RemoveComponent<AgentGrounding>(m_Entity);
        }

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<AgentGrounding>(m_Entity, true);
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<AgentGrounding>(m_Entity, false);
        }
    }

    class AgentGroundingBaker : Baker<AgentGroundingAuthoring>
    {
        public override void Bake(AgentGroundingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, authoring.DefaultGrounding);
            if (authoring.Mode == AgentGroundingMode.BodyAndForce)
                AddComponent<AgentGroundingSlope>(entity);
        }
    }
}
