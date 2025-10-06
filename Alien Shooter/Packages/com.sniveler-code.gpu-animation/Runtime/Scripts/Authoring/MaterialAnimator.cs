using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

using SnivelerCode.GpuAnimation.Scripts.Components;

namespace SnivelerCode.GpuAnimation.Scripts.Authoring
{
    public class MaterialAnimatorAuthoring : MonoBehaviour
    {
        public int bonesCount;
        public List<MaterialAnimatorBake> animations;
        public List<bool> alphas;
        
        class MaterialAnimatorBaker : Baker<MaterialAnimatorAuthoring>
        {
            public override void Bake(MaterialAnimatorAuthoring data)
            {
                using var builder = new BlobBuilder(Allocator.Temp);
                ref var blobAsset = ref builder.ConstructRoot<MaterialAnimatorBlobAsset>();
                blobAsset.BoneCount = (byte)data.bonesCount;
                var animationArray = builder.Allocate(ref blobAsset.Animations, data.animations.Count);
                for (var i = 0; i < animationArray.Length; ++i)
                {
                    var animation = data.animations[i];
                    animationArray[i] = animation.ToBlobAsset();
                    var transitionArray = builder.Allocate(ref animationArray[i].Transitions, animation.transitions.Count);
                    for (var k = 0; k < transitionArray.Length; ++k)
                    {
                        transitionArray[k] = animation.transitions[k].ToBlobAsset();
                    }
                }
            
                var alphaArray = builder.Allocate(ref blobAsset.Alphas, data.alphas.Count);
                for (var i = 0; i < alphaArray.Length; i++)
                {
                    alphaArray[i] = data.alphas[i];
                }
            
                var blobAssetReference = builder.CreateBlobAssetReference<MaterialAnimatorBlobAsset>(Allocator.Persistent);
                AddComponent(new MaterialAnimatorBlobData { Value = blobAssetReference });
            }
        }
    }
}
