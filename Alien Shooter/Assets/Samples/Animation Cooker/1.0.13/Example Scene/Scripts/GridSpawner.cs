// GridSpawner - A class that can spawn any number of entities in a grid. It's mainly used for testing purposes.
//
// You typically will need a Text field, an Input field, and a Button in your GUI and then attach those to this class,
// which allows the player to spawn things at runtime.
//
// This class will spawn any entities it finds that have a AnimationStateData or SpawnTag component attached to them.
// As entities are spawned, they will be setup to play different animation clips.
//--------------------------------------------------------------------------------------------------//

using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using TMPro;
using AnimCooker;

namespace AnimationCookerExample
{

public class GridSpawner : MonoBehaviour
{
    [TextArea]
    public string m_info = "This spawner will spawn any items placed in a subscene that have AnimationStateData or SpawnTag.";

    [Tooltip("Spacing between entities (default 1.5)")] public float m_spacing = 2f;
    [Tooltip("A text object that will get updated with the current spawn count (optional)")] public TextMeshProUGUI m_statusText = null;
    [Tooltip("An input field object that lets the player enter the spawn count")] public TMP_InputField m_inputField = null;
    [Tooltip("If set to true, each instance's animation speed will vary randomly.")] public bool m_enableVarySpeed = false;
    [Tooltip("If set to true, alternate animation clips for each spawn.")] public bool m_enableVaryClip = false;

    EntityManager m_em;
    float3 m_pos = float3.zero;
    float m_width = 0f;
    int m_spawnCount = 0;
    AnimDbRefData m_db;
    bool m_isInitialized = false;
    bool m_isDbValid = false;

    // deletes any entity that has a SpawnedTag
    public void ClearSpawns()
    {
        EntityQuery query = m_em.CreateEntityQuery(ComponentType.ReadOnly<SpawnedTag>());
        if (query.CalculateEntityCount() > 0) { m_em.DestroyEntity(query); }
        UpdateCountText();
    }

    public void BatchSpawn()
    {
        if (!m_isInitialized) {
            m_em = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery q = m_em.CreateEntityQuery(ComponentType.ReadOnly<AnimDbRefData>());
            if (!q.TryGetSingleton(out m_db)) {  m_isDbValid = true; }
            m_isInitialized = true;
        }

        ClearSpawns();
        EntityQueryDesc queryDesc = new EntityQueryDesc { Any = new ComponentType[]{ ComponentType.ReadOnly<SpawnTag>(), ComponentType.ReadOnly<AnimationStateData>() } };
        EntityQuery query = m_em.CreateEntityQuery(queryDesc);
        NativeArray<Entity> prefabs = query.ToEntityArray(Allocator.Temp);
        if (prefabs.Length <= 0) {
            m_statusText.text = "There must be a singletone entity with an AnimationStateData or SpawnTag in the subscene.";
            UnityEngine.Debug.Log(m_statusText.text);
            return;
        }

        int count = 0;
        m_pos = transform.position;
        m_spawnCount = int.Parse(m_inputField.text);
        m_width = math.sqrt(m_spawnCount) * m_spacing;
        m_pos.x -= m_width * 0.5f;
        m_pos.z -= m_width * 0.5f;
        int wholePart = m_spawnCount / prefabs.Length;
        int remainder = m_spawnCount - (wholePart * prefabs.Length);
        // for every prefab, spawn wholePart instances
        for (int i = 0; i < prefabs.Length; i++) {
            // if we're on the last index, include the remainder
            if (i == prefabs.Length - 1) { wholePart += remainder; }

            // do a batch instantiation for this row
            NativeArray<Entity> entities = m_em.Instantiate(prefabs[i], wholePart, Allocator.Temp);

            // set the position for each of the new entities in this row
            // also change color, animation clip, and speed based on user options
            for (int j = 0; j < entities.Length; j++) {
                Entity entity = entities[j];

                if (m_isDbValid) {
                    if (m_em.HasComponent<AnimationStateData>(entity)) {
                        short modelIndex = m_em.GetComponentData<AnimationStateData>(entity).ModelIndex;
                        // this will cycle through clip indexes (optional depending on which variances are used)
                        //int clipIdx = j % m_db.GetModel(modelIndex).Clips.Length;
                        int clipIdx = j % m_db.GetModel(modelIndex).Clips.Length;

                        // Get a speed. Use a random speed if specified, and otherwise use the default of 1.
                        float speedMultiplier = m_enableVarySpeed ? UnityEngine.Random.Range(0.5f, 2f) : 1f;

                        if (m_enableVaryClip) {
                            // set the animation for this clip (also sets the speed)
                            m_em.SetComponentData(entity, new AnimationCmdData() { ClipIndex = (byte)clipIdx, Cmd = AnimationCmd.PlayOnce, Speed = speedMultiplier });
                        } else if (m_enableVarySpeed) {
                            m_em.SetComponentData(entity, new AnimationSpeedData { PlaySpeed = speedMultiplier });
                        }
                    }
                }

                // move this entity to its location in the grid

                LocalTransform xForm = m_em.GetComponentData<LocalTransform>(entity);
                xForm.Position = m_pos;
                m_em.SetComponentData<LocalTransform>(entity, xForm);

                m_em.AddComponent<SpawnedTag>(entity);
                IncrementPosition(ref m_pos);
            }
            count += entities.Length;
            entities.Dispose();
        }
        UpdateCountText();
    }

    // refreshes the total number of entities spawned
    public void UpdateCountText()
    {
        if (m_statusText == null) { return; }
        EntityQuery query = m_em.CreateEntityQuery(ComponentType.ReadOnly<SpawnedTag>());
        m_statusText.text = $"{query.CalculateEntityCount()} entities";
    }

    // increments the position to the next slot such that the spawner is in the middle of the spawn area
    void IncrementPosition(ref float3 pos)
    {
        // increment position
        pos.x += m_spacing;
        if (pos.x > ((m_width * 0.5f) + transform.position.x)) {
            pos.x = transform.position.x - (m_width * 0.5f);
            pos.z += m_spacing;
        }
    }
}

} // namespace