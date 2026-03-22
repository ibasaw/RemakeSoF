Shader "SoF2/MapSurface"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _LightBlend ("Light Blend (0=Unlit, 1=Full Lit)", Range(0.0, 1.0)) = 0.25
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // =====================================================
        // Forward Pass: Unlit-Basis + anteilige Lambert-Beleuchtung
        // =====================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;
                half _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Basistextur (= gebackene SoF2-Beleuchtung enthalten)
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                #ifdef _ALPHATEST_ON
                    clip(texColor.a - _Cutoff);
                #endif

                // Wenn kein Lichtanteil gewuenscht: pure Unlit
                if (_LightBlend <= 0.001)
                {
                    texColor.rgb = MixFog(texColor.rgb, input.fogFactor);
                    return texColor;
                }

                // Beleuchtung berechnen (Lambert)
                float3 normalWS = normalize(input.normalWS);

                // Main Light (Sonne)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 mainLighting = mainLight.color * NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                // Additional Lights (Point/Spot aus MapLightApplier)
                half3 additionalLighting = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < pixelLightCount; i++)
                    {
                        Light additionalLight = GetAdditionalLight(i, input.positionWS);
                        half addNdotL = saturate(dot(normalWS, additionalLight.direction));
                        additionalLighting += additionalLight.color * addNdotL
                            * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    }
                #elif defined(_ADDITIONAL_LIGHTS_VERTEX)
                    // Vertex-Lighting Fallback: weniger genau, aber funktioniert immer
                    additionalLighting = half3(0.1, 0.1, 0.1);
                #endif

                // SH-Ambient: minimale Fuellung dunkler Bereiche
                // (gesteuert ueber RenderSettings.ambientLight / surfacelight)
                half3 ambient = SampleSH(normalWS);

                // Gesamtes Licht: Ambient + Directional + Additional (Point/Spot)
                // Clamp auf 2.0 verhindert Hotspots nahe der Lichtquelle:
                // URP 1/d²-Falloff geht bei Abstand~0 gegen unendlich,
                // aber in idTech3/SoF2 war das Licht per Lightmap gebacken
                // und konnte nie ueber ~2x Original-Helligkeit gehen.
                half3 totalLight = min(mainLighting + additionalLighting + ambient, 5.0);

                // idTech3-Stil: Textur-Basis wird proportional zu _LightBlend abgedunkelt.
                // Ohne Licht: texColor * (1 - _LightBlend) = 75% bei Blend=0.25 (leicht dunkler).
                // Mit Licht: texColor * (baseDarkening + _LightBlend * totalLight) → heller.
                // Das erzeugt sichtbaren Kontrast zwischen beleuchteten/unbeleuchteten Bereichen,
                // statt nur auf eine bereits helle Textur draufzuaddieren.
                half baseDarkening = 1.0 - _LightBlend;
                half3 finalColor = texColor.rgb * (baseDarkening + _LightBlend * totalLight);

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }

        // =====================================================
        // Shadow Caster Pass: Map-Geometry wirft Schatten
        // =====================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;
                half _Cutoff;
            CBUFFER_END

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 _LightDirection;

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    clip(texColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // =====================================================
        // Depth Only Pass: Fuer Depth-Prepass
        // =====================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;
                half _Cutoff;
            CBUFFER_END

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    clip(texColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
