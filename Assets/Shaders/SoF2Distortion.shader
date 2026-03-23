Shader "SoF2/Distortion"
{
    Properties
    {
        _DistortionMap ("Distortion Normal Map", 2D) = "bump" {}
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.2)) = 0.05
        _DistortionSpeed ("Scroll Speed", Range(0.0, 2.0)) = 0.3
        _DistortionTiling ("Tiling", Range(0.5, 5.0)) = 1.5
        _Opacity ("Opacity (0=Invisible, 1=Visible)", Range(0.0, 1.0)) = 0.0
        _TintColor ("Heat Tint", Color) = (1, 0.95, 0.85, 1)

        _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+50"
            "IgnoreProjector" = "True"
        }

        // =====================================================
        // SoF2 Distortion/Heat-Haze Pass
        //
        // Liest die Szenen-Farbe hinter dem Partikel und verschiebt
        // die UV-Koordinaten basierend auf einer Normal-Map.
        // Erzeugt den typischen Hitze-Schlieren-Effekt nach Explosionen.
        //
        // Wird als ParticleSystem-Material verwendet:
        // - Burst spawn nach Explosion
        // - Groesse wachst, Alpha fadet aus
        // - Vertex-Color Alpha steuert Distortion-Staerke
        //
        // Benoetigt: URP Opaque Texture aktiviert
        // (Project Settings → URP Asset → Opaque Texture = ON)
        // =====================================================
        Pass
        {
            Name "DistortionForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
                float4 screenPos : TEXCOORD2;
            };

            TEXTURE2D(_DistortionMap);
            SAMPLER(sampler_DistortionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _DistortionMap_ST;
                half _DistortionStrength;
                half _DistortionSpeed;
                half _DistortionTiling;
                half _Opacity;
                half4 _TintColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.color = input.color;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Partikel-Alpha steuert Distortion-Staerke (fadet mit Lifetime aus)
                float particleAlpha = input.color.a;

                // Scrollende UV fuer animierte Verzerrung
                float2 distUV = input.uv * _DistortionTiling;
                distUV += _Time.y * _DistortionSpeed * float2(0.7, 0.3);

                // Normal-Map Sample: XY = Distortion-Offset
                float3 normalSample = UnpackNormal(SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, distUV));

                // Zweite Schicht (gegenlaeuft) fuer realistischere Turbulenz
                float2 distUV2 = input.uv * _DistortionTiling * 0.7;
                distUV2 -= _Time.y * _DistortionSpeed * float2(0.4, 0.6);
                float3 normalSample2 = UnpackNormal(SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, distUV2));

                // Kombiniere beide Schichten
                float2 distortion = (normalSample.xy + normalSample2.xy) * 0.5;
                distortion *= _DistortionStrength * particleAlpha;

                // Screen-UV + Distortion-Offset
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 distortedUV = screenUV + distortion;

                // Clamp um Artefakte am Bildschirmrand zu vermeiden
                distortedUV = clamp(distortedUV, 0.001, 0.999);

                // Scene Color hinter dem Partikel samplen
                half3 sceneColor = SampleSceneColor(distortedUV);

                // Leichter Waerme-Tint
                sceneColor *= lerp(half3(1, 1, 1), _TintColor.rgb, particleAlpha * 0.3);

                // Alpha: Sichtbarkeit des Distortion-Effekts
                // Bei _Opacity=0 ist nur die Verzerrung sichtbar (reiner Refraktions-Effekt)
                half alpha = saturate(particleAlpha * max(_Opacity, length(distortion) * 10.0));

                return half4(sceneColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
