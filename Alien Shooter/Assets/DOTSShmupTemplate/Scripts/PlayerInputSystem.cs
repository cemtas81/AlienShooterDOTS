using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerInputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var input = new PlayerInput
        {
            Move = float2.zero,
            Fire = false
        };

        input.Move.x = Input.GetAxisRaw("Horizontal");
        input.Move.y = Input.GetAxisRaw("Vertical");
        input.Fire = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var playerEntity in SystemAPI.QueryBuilder().WithAll<PlayerTag>().Build().ToEntityArray(Allocator.Temp))
        {
            ecb.SetComponent(playerEntity, input);
        }

        ecb.Playback(state.EntityManager);
    }
}