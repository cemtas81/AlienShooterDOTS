#ifndef GA_FUQUNA_VATBAKER_VAT_INCLUDED
#define GA_FUQUNA_VATBAKER_VAT_INCLUDED

// Deðiþken tanýmlarýný geri koyuyoruz
sampler2D _VatPositionTex;
float4 _VatPositionTex_TexelSize;

sampler2D _VatNormalTex;
float4 _VatNormalTex_TexelSize;

// UNITY_CBUFFER_* makrolarý tanýmlýysa cbuffer içine alýyoruz, deðilse normal deðiþken tanýmý yapýyoruz.
#if defined(UNITY_CBUFFER_START) && defined(UNITY_CBUFFER_END)
    UNITY_CBUFFER_START(UnityPerMaterial)
        float _VatAnimFps;
        float _VatAnimLength;
    UNITY_CBUFFER_END
#else
    float _VatAnimFps;
    float _VatAnimLength;
#endif

inline float CalcVatAnimationTime(float time)
{
    return (time % _VatAnimLength) * _VatAnimFps;
}

inline float4 CalcVatTexCoord(uint vertexId, float animationTime)
{
    float x = vertexId + 0.5;
    float y = animationTime + 0.5;
    
    return float4(x, y, 0, 0) * _VatPositionTex_TexelSize;   
}

inline float3 GetVatPosition(uint vertexId, float animationTime)
{
    return (float3)tex2Dlod(_VatPositionTex, CalcVatTexCoord(vertexId, animationTime));
}

inline float3 GetVatNormal(uint vertexId, float animationTime)
{
    return (float3)tex2Dlod(_VatNormalTex, CalcVatTexCoord(vertexId, animationTime));
}

#endif