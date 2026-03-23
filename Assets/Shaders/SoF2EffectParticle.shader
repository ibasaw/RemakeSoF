Shader "SoF2/EffectParticle"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0.5, 10.0)) = 1.5
        _SoftParticleRange ("Soft Particle Range", Range(0.01, 5.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha ("Src Alpha", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha ("Dst Alpha", Float) = 1
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
        // Particle Forward Pass: Soft Particles + HDR Emission
        // Soft Particles: Partikel blenden aus wenn sie Szenen-
        // Geometrie schneiden (kein harter Clip an Waenden/Boden).
        // HDR Emission: Werte > 1.0 lassen Bloom-Post-Processing
        // den Effekt zum Leuchten bringen (Tracer, Explosionen).
        // =====================================================
        Pass
        {
            Name "ParticleForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float4 projPos : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _EmissionIntensity;
                half _SoftParticleRange;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                // Screen-Position fuer Depth-Vergleich (Soft Particles)
                output.projPos = ComputeScreenPos(output.positionCS);
                output.projPos.z = -TransformWorldToView(vertexInput.positionWS).z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 vertColor = input.color;

                // Farbe: Textur × Basis × Vertex × HDR-Emission
                half3 color = texColor.rgb * _BaseColor.rgb * vertColor.rgb * _EmissionIntensity;
                half alpha = texColor.a * _BaseColor.a * vertColor.a;

                // Soft Particles: Tiefenvergleich mit Szenen-Geometrie.
                // Partikel nahe an Waenden/Boden werden sanft ausgeblendet
                // statt hart abgeschnitten. Wenn kein Depth-Texture vorhanden,
                // ist sceneDepth sehr gross → softFade=1.0 → kein Effekt.
                float2 screenUV = input.projPos.xy / input.projPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragDepth = input.projPos.z;
                float depthDiff = sceneDepth - fragDepth;
                float softFade = saturate(depthDiff / _SoftParticleRange);

                alpha *= softFade;
                // Farbe auch faden (wichtig fuer Additive Blending)
                color *= softFade;

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
