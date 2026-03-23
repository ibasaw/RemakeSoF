Shader "SoF2/ProjectileTrail"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(1.0, 10.0)) = 2.0
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.3
        _CoreBrightness ("Core Brightness", Range(1.0, 8.0)) = 2.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        // =====================================================
        // Trail Forward Pass: Soft-Edge Glow mit HDR Emission
        // Weiches Auslaufen an den Raendern statt harter Silhouette.
        // Heller Kern (CoreBrightness) fuer Bloom-Post-Processing.
        // Vertex-Color kommt vom TrailRenderer-Gradient.
        // =====================================================
        Pass
        {
            Name "TrailForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _EmissionIntensity;
                half _EdgeSoftness;
                half _CoreBrightness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 vertColor = input.color;

                // Soft Edge: V-Koordinate 0→1 ueber die Trail-Breite.
                // Mitte (0.5) = volle Helligkeit, Raender (0, 1) = ausgeblendet.
                half edgeDist = abs(input.uv.y - 0.5) * 2.0;
                half edgeFade = 1.0 - smoothstep(1.0 - _EdgeSoftness * 2.0, 1.0, edgeDist);

                // Core Glow: Kern heller als Rand (quadratischer Falloff).
                // CoreBrightness=2.5 → Mitte 2.5× heller, Rand 1×.
                half coreFactor = lerp(_CoreBrightness, 1.0, edgeDist * edgeDist);

                // Final: Textur × Basis × Vertex × Emission × Core
                half3 color = texColor.rgb * _BaseColor.rgb * vertColor.rgb
                    * _EmissionIntensity * coreFactor;
                half alpha = texColor.a * _BaseColor.a * vertColor.a * edgeFade;

                // Edge-Fade auf Farbe anwenden (wichtig fuer Additive Blending,
                // da dort Alpha ignoriert wird und nur RGB zaehlt)
                color *= edgeFade;

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
