using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

// Simple struct to store input direction
public struct PlayerInput : IComponentData
{
    public float2 Move;
    public bool Fire;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerInputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Query for player input singleton (you can extend this for multiplayer/local)
        var input = new PlayerInput
        {
            Move = float2.zero,
            Fire = false
        };

        // Input polling (expand as needed for your input system)
        input.Move.x = Input.GetAxisRaw("Horizontal");
        input.Move.y = Input.GetAxisRaw("Vertical");
        input.Fire = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");

        // Update all player entities
        foreach (var playerEntity in SystemAPI.QueryBuilder().WithAll<PlayerTag>().Build().ToEntityArray(Allocator.Temp))
        {
            state.EntityManager.SetComponentData(playerEntity, input);
        }
    }
}