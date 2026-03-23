Shader "SoF2/Decal"
{
    Properties
    {
        _BaseMap ("Decal Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)

        [Header(Depth Bias)]
        _DepthBias ("Depth Offset (Z-Fighting)", Range(-1.0, 0.0)) = -0.001

        _Cull ("Cull Mode", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-10"
            "IgnoreProjector" = "True"
        }

        // =====================================================
        // SoF2 Decal Pass: Scorch-Marks, Einschusslocher, Blutflecken
        //
        // Alpha-Blending (kein Additive), kein Lighting.
        // Decals sind "gebackene" Texturen die auf Oberflaechen projiziert werden.
        // Depth-Bias verhindert Z-Fighting mit der darunterliegenden Oberflaeche.
        // ZWrite OFF damit Decals sich nicht gegenseitig abschneiden.
        // =====================================================
        Pass
        {
            Name "DecalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset [_DepthBias], [_DepthBias]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _DepthBias;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = texColor * _BaseColor * input.color;

                // Alpha-Clip: voellig transparente Pixel verwerfen
                clip(color.a - 0.004);

                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
