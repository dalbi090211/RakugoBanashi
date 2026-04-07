Shader "Custom/Pixelate"
{
    Properties 
    { 
        _PixelSize("Pixel Size", Float) = 32
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            //(2,0), (0, 0), (0, 2)로 그려진 커다란 삼각형을 그리고 이를 통해 픽셀마다 uv 좌표를 생성.(전체 크기는 1, 1 이기에 전체좌표의 픽셀을 생성 가능, 작은 삼각형 2개를 그리는 방식도 가능)
            Varyings vert(uint vertexID : SV_VertexID)  // ✅ 핵심
            {
                Varyings OUT;
                OUT.uv = float2((vertexID << 1) & 2, vertexID & 2); //0일 때 (0, 0) 생성, 1일 때 (2, 0) 생성, 2일 때 (0, 2) 생성
                OUT.positionCS = float4(OUT.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                OUT.uv.y = 1.0 - OUT.uv.y;
                #endif
                return OUT;
            }

            //가져온 uv좌표에 맞춰서 양자화
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = floor(IN.uv * _PixelSize) / _PixelSize;
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
            }
            ENDHLSL
        }
    }
}