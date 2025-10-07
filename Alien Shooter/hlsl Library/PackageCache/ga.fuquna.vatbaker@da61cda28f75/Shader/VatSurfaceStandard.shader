Shader "VatBaker/VatSurfaceStandard"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _NormalTex("NormalTex", 2D) = "white" {}
        _AnimationTimeOffset("AnimationTimeOffset", float) = 0.0
        _VatPositionTex ("VatPositionTex", 2D) = "white" {}
        _VatNormalTex ("VatNormalTex", 2D) = "white" {}
        _VatAnimFps("VatAnimFps", float) = 5.0
        _VatAnimLength("VatAnimLength", float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert addshadow
        #pragma instancing_options procedural:setup
        #pragma target 3.5
        
        // DOTS/Entity Graphics desteði
        #pragma multi_compile _ DOTS_INSTANCING_ON
        
        // VAT deðiþkenlerini özel olarak yönettiðimizi belirtelim
        #define VAT_CUSTOM_VARIABLES
        
        // SRP Batcher için tüm özellikleri tek bir CBUFFER içine koyalým
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NormalTex_ST;
            float _VatAnimFps;
            float _VatAnimLength;
            float4 _VatPositionTex_TexelSize;
            float4 _VatNormalTex_TexelSize;
        CBUFFER_END
        
        // Texture samplerlar CBUFFER dýþýnda olmalý
        sampler2D _MainTex;
        sampler2D _NormalTex;
        sampler2D _VatPositionTex;
        sampler2D _VatNormalTex;
        
        // Include dosyasýný deðiþkenler tanýmlandýktan sonra ekleyelim
        #include "Vat.hlsl"

        struct appdata
        {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            fixed4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            uint vId : SV_VertexID;
        };
        
        struct Input
        {
           float2 uv_MainTex;
           float2 uv_NormalTex;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
           UNITY_DEFINE_INSTANCED_PROP(float, _AnimationTimeOffset)
        UNITY_INSTANCING_BUFFER_END(Props)
        
        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                // Procedural instancing için gerekli setup
                unity_ObjectToWorld = unity_ObjectToWorld; // Dummy atama
            #endif
        }
        
        void vert (inout appdata v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            
            float animOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationTimeOffset);
            float animationTime = CalcVatAnimationTime(_Time.y + animOffset);
            v.vertex.xyz = GetVatPosition(v.vId, animationTime);
            v.normal.xyz = GetVatNormal(v.vId, animationTime);
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex);
            o.Normal = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex));
        }
        
        ENDCG
    }
}