Shader "SoF2/Metal"
{
    Properties
    {
        _BaseMap ("Metal Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _LightBlend ("Light Blend (0=Unlit, 1=Full Lit)", Range(0.0, 1.0)) = 0.7

        [Header(Metallic Specular)]
        _SpecularPower ("Specular Sharpness", Range(8.0, 512.0)) = 64.0
        _SpecularIntensity ("Specular Intensity", Range(0.0, 5.0)) = 1.5
        _Metallic ("Metallic Factor", Range(0.0, 1.0)) = 0.8

        [Header(Environment Reflection)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 5.0
        _ReflectionStrength ("Reflection Strength", Range(0.0, 1.0)) = 0.15

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
        // SoF2 Metal Pass: Lambert + Blinn-Phong Specular
        //
        // Metallische Oberflaechen reflektieren Licht schaerfer
        // als Standard-Surfaces. Specular-Highlight + subtile
        // Fresnel-Aufhellung am Rand simuliert Metall-Charakter.
        // Basis-Beleuchtung identisch zu SoF2MapSurface.
        // =====================================================
        Pass
        {
            Name "MetalForward"
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
            #pragma multi_compile _ _FORWARD_PLUS
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
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;
                half _SpecularPower;
                half _SpecularIntensity;
                half _Metallic;
                half _FresnelPower;
                half _ReflectionStrength;
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
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                #ifdef _ALPHATEST_ON
                    clip(texColor.a - _Cutoff);
                #endif

                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // Unlit fast-path
                if (_LightBlend <= 0.001)
                {
                    texColor.rgb = MixFog(texColor.rgb, input.fogFactor);
                    return texColor;
                }

                // === Lambert + Blinn-Phong Specular ===
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 mainLighting = mainLight.color * NdotL
                    * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                // Specular: metallische Oberflaechen nutzen Texturfarbe als Specular-Color
                half3 specColor = lerp(half3(0.04, 0.04, 0.04), texColor.rgb, _Metallic);
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                half3 specularLight = specColor * mainLight.color
                    * pow(NdotH, _SpecularPower) * _SpecularIntensity
                    * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                half3 additionalLighting = half3(0, 0, 0);
                half3 additionalSpec = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                        half addNdotL = saturate(dot(normalWS, addLight.direction));
                        additionalLighting += addLight.color * addNdotL
                            * addLight.distanceAttenuation * addLight.shadowAttenuation;

                        float3 addHalf = normalize(addLight.direction + viewDir);
                        float addNdotH = saturate(dot(normalWS, addHalf));
                        additionalSpec += specColor * addLight.color
                            * pow(addNdotH, _SpecularPower) * _SpecularIntensity * 0.5
                            * addLight.distanceAttenuation * addLight.shadowAttenuation;
                    LIGHT_LOOP_END
                #endif

                half3 ambient = SampleSH(normalWS);
                half3 totalLight = min(mainLighting + additionalLighting + ambient, 5.0);

                // === Fresnel-Rim fuer Metall-Kanten ===
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _ReflectionStrength;

                // === Zusammenbauen ===
                half baseDarkening = 1.0 - _LightBlend;
                half3 finalColor = texColor.rgb * (baseDarkening + _LightBlend * totalLight);

                // Specular + Fresnel-Rim hinzufuegen
                finalColor += specularLight + additionalSpec;
                finalColor += fresnel * specColor;

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }

        // =====================================================
        // Shadow Caster Pass
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
                half _SpecularPower;
                half _SpecularIntensity;
                half _Metallic;
                half _FresnelPower;
                half _ReflectionStrength;
                half _Cutoff;
            CBUFFER_END

            float3 _LightDirection;

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
        // Depth Only Pass
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
                half _SpecularPower;
                half _SpecularIntensity;
                half _Metallic;
                half _FresnelPower;
                half _ReflectionStrength;
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

    FallBack "SoF2/MapSurface"
}
