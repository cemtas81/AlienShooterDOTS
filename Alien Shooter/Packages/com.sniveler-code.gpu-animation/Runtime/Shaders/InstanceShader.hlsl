#ifndef GPU_INSTANCE_INCLUDED
#define GPU_INSTANCE_INCLUDED

float4 get_sampler(UnityTexture2D anim_texture, float index)
{
    float row = index * anim_texture.texelSize.x;
    float col = index % (uint)anim_texture.texelSize.z;
    float2 uv_coords = float2(col * anim_texture.texelSize.x, row * anim_texture.texelSize.y);
    return anim_texture.SampleLevel(anim_texture.samplerstate, uv_coords, 0);   
}

float4x4 get_matrix(
    UnityTexture2D anim_texture1,
    UnityTexture2D anim_texture2,
    UnityTexture2D anim_texture3,
    const int frame,
    const int bone)
{
    const int matrix_index = frame + bone;
    float4 row0 = get_sampler(anim_texture1, matrix_index);
    float4 row1 = get_sampler(anim_texture2, matrix_index);
    float4 row2 = get_sampler(anim_texture3, matrix_index);
    return float4x4(row0, row1, row2, float4(0, 0, 0, 1));
}

void GetMatrix_float(
    const float4 position,
    const float4 normal,
    half4 bone_index, 
    float4 bone_weight,
    const float3 frames,
    UnityTexture2D at1,
    UnityTexture2D at2,
    UnityTexture2D at3,
    out float4 anim_position,
    out float4 anim_normal
    )
{
    float4x4 bone1_matrix = get_matrix(at1, at2, at3, (uint)frames.x, bone_index.x);
    float4x4 bone2_matrix = get_matrix(at1, at2, at3, (uint)frames.x, bone_index.y);
    float4x4 bone3_matrix = get_matrix(at1, at2, at3, (uint)frames.x, bone_index.z);
    float4x4 bone4_matrix = get_matrix(at1, at2, at3, (uint)frames.x, bone_index.w);

    const float4x4 combined_first =
        bone1_matrix * bone_weight.x +
            bone2_matrix * bone_weight.y +
                bone3_matrix * bone_weight.z +
                    bone4_matrix * bone_weight.w;
    
    const float4 current_position = mul(combined_first, position);
    const float4 current_normal = mul(combined_first, float4(normal.xyz, 0));
        
    bone1_matrix = get_matrix(at1, at2, at3, (uint)frames.y, bone_index.x);
    bone2_matrix = get_matrix(at1, at2, at3, (uint)frames.y, bone_index.y);
    bone3_matrix = get_matrix(at1, at2, at3, (uint)frames.y, bone_index.z);
    bone4_matrix = get_matrix(at1, at2, at3, (uint)frames.y, bone_index.w);

    const float4x4 combined_second =
        bone1_matrix * bone_weight.x +
            bone2_matrix * bone_weight.y +
                bone3_matrix * bone_weight.z +
                    bone4_matrix * bone_weight.w;
    
    const float4 next_position = mul(combined_second, position);
    const float4 next_normal = mul(combined_second, float4(normal.xyz, 0));
    
    anim_position = lerp(current_position, next_position, frames.z);
    anim_normal = lerp(current_normal, next_normal, frames.z);
}

#endif