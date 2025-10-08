//using AnimCooker;
//using ProjectDawn.Navigation;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using UnityEngine;

//[BurstCompile]
//[UpdateInGroup(typeof(SimulationSystemGroup))] // Komutlar� simulation'da yaz; playback presentation'da okur
//[UpdateAfter(typeof(EnemyAgentMovementSystem))] // AgentBody.IsStopped g�ncellendikten sonra �al��


//public partial struct AnimationPlaySystem : ISystem
//{
//    void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<AnimDbRefData>(); // en az bir DB bekle
//    }

//    [BurstCompile]
//    void OnUpdate(ref SystemState state)
//    {
//        // Sahnedeki ilk AnimDbRefData'y� g�venle al
//        AnimDbRefData db = default;
//        int foundCount = 0;
//        foreach (var dbRef in SystemAPI.Query<RefRO<AnimDbRefData>>())
//        {
//            if (foundCount == 0) db = dbRef.ValueRO;
//            foundCount++;
//            if (foundCount > 1) break;
//        }

//        if (foundCount == 0)
//            return;

//        if (foundCount > 1)
//            Debug.LogWarning($"Birden fazla AnimDbRefData bulundu ({foundCount}). �lk bulunana g�re davran�l�yor.");

//        // Anahtar (main thread)
//        var moveKey = new FixedString128Bytes("RifleRun");
//        /*const float moveThreshold = 0.05f;*/ // art�k velocity e�i�i kullan�lm�yor ama b�rak�labilir

//        // Sorgu: EnemyTag ve gerekli animasyon bile�enlerine sahip entitiler
//        foreach (var (cmdRef, speedRef, stateRef, bodyRef) in
//            SystemAPI.Query<RefRW<AnimationCmdData>, RefRW<AnimationSpeedData>, RefRO<AnimationStateData>, RefRO<AgentBody>>()
//                     .WithAll<EnemyTag>())
//        {
//            var animState = stateRef.ValueRO;
//            var body = bodyRef.ValueRO;

//            // Basit mant�k: agent hedefte de�ilse (IsStopped == false) hareket animasyonunu oynat
//            float speedSq = math.lengthsq(body.Velocity);
//            bool moving = speedSq > 0.01f && !body.IsStopped; // gerekirse !body.IsStopped'� kald�r

//            if (moving)
//            {
//                // play speed basit�e 1 (ya da ihtiya� varsa body.Velocity kullanarak ayarla)
//                speedRef.ValueRW.PlaySpeed = 1f;

//                short clipIndex = -1;
//                ref var model = ref db.Ref.Value.Models[animState.ModelIndex];

//                clipIndex = model.FindClipThatContains(moveKey);

//                if (clipIndex >= 0)
//                {
//                    if (cmdRef.ValueRW.ClipIndex != clipIndex || cmdRef.ValueRW.Cmd != AnimationCmd.SetPlayForever)
//                    {
//                        cmdRef.ValueRW.ClipIndex = clipIndex;
//                        cmdRef.ValueRW.Cmd = AnimationCmd.SetPlayForever;
//                        cmdRef.ValueRW.Speed = 1f;
//                    }
//                }
//                else
//                {
//                    // move klibi yoksa en az�ndan play komutunu tetikle
//                    cmdRef.ValueRW.Cmd = AnimationCmd.SetPlayForever;
//                }
//            }
//            else
//            {
//                // Agent durduysa baker taraf�ndan ayarlanan forever klibe d�n
//                if (animState.ForeverClipIndex >= 0 && cmdRef.ValueRW.ClipIndex != animState.ForeverClipIndex)
//                {
//                    cmdRef.ValueRW.ClipIndex = animState.ForeverClipIndex;
//                    cmdRef.ValueRW.Cmd = AnimationCmd.SetPlayForever;
//                    cmdRef.ValueRW.Speed = 1f;
//                }
//            }
//        }
//    }
//}