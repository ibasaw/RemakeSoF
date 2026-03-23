Shader "SoF2/Polished"
{
    Properties
    {
        _BaseMap ("Surface Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _LightBlend ("Light Blend (0=Unlit, 1=Full Lit)", Range(0.0, 1.0)) = 0.7

        [Header(Specular Gloss)]
        _SpecularPower ("Specular Sharpness", Range(8.0, 512.0)) = 96.0
        _SpecularIntensity ("Specular Intensity", Range(0.0, 5.0)) = 1.0
        _Glossiness ("Glossiness (fresnel blend)", Range(0.0, 1.0)) = 0.5

        [Header(Fresnel Rim)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 4.0
        _ReflectionStrength ("Reflection Strength", Range(0.0, 1.0)) = 0.1
        _ReflectionTint ("Reflection Tint", Color) = (1, 1, 1, 1)

        _Cull ("Cull Mode", Float) = 2
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
        // SoF2 Polished Pass: Marmor / Fliesen / polierte Steinoberflaechen
        //
        // Opaker Shader mit hartem Specular-Highlight.
        // Fresnel-Rim simuliert die Wachsglanz-Reflektion
        // polierter Steinoberflaechen bei flachen Winkeln.
        // Lambert-Beleuchtung identisch zu MapSurface.
        // =====================================================
        Pass
        {
            Name "PolishedForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

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
                half _Glossiness;
                half _FresnelPower;
                half _ReflectionStrength;
                half4 _ReflectionTint;
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
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // === Fresnel: polierte Oberflaechen reflektieren bei flachem Winkel ===
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _ReflectionStrength;

                // === Beleuchtung (identisch zu MapSurface Basis) ===
                half3 totalLight = half3(0, 0, 0);
                half3 specularLight = half3(0, 0, 0);

                if (_LightBlend > 0.001)
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    half NdotL = saturate(dot(normalWS, mainLight.direction));
                    half3 mainLighting = mainLight.color * NdotL
                        * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                    // Blinn-Phong Specular
                    float3 halfDir = normalize(mainLight.direction + viewDir);
                    float NdotH = saturate(dot(normalWS, halfDir));
                    specularLight += mainLight.color * pow(NdotH, _SpecularPower)
                        * _SpecularIntensity * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                    half3 additionalLighting = half3(0, 0, 0);
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
                            specularLight += addLight.color * pow(addNdotH, _SpecularPower)
                                * _SpecularIntensity * 0.5
                                * addLight.distanceAttenuation * addLight.shadowAttenuation;
                        LIGHT_LOOP_END
                    #endif

                    half3 ambient = SampleSH(normalWS);
                    totalLight = min(mainLighting + additionalLighting + ambient, 5.0);
                }

                // === Zusammenbauen ===
                half baseDarkening = 1.0 - _LightBlend;
                half3 litFactor = baseDarkening + _LightBlend * totalLight;

                half3 color = texColor.rgb * _BaseColor.rgb * litFactor;

                // Fresnel-Reflektion: leichter Schimmer bei flachen Winkeln
                color += fresnel * _ReflectionTint.rgb * _Glossiness;

                // Specular-Highlights: harte Lichtpunkte auf polierter Oberflaeche
                color += specularLight;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // =====================================================
        // Shadow Caster
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;
                half _SpecularPower;
                half _SpecularIntensity;
                half _Glossiness;
                half _FresnelPower;
                half _ReflectionStrength;
                half4 _ReflectionTint;
            CBUFFER_END

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                posWS = ApplyShadowBias(posWS, normWS, _LightDirection);
                output.positionCS = TransformWorldToHClip(posWS);
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;
                half _SpecularPower;
                half _SpecularIntensity;
                half _Glossiness;
                half _FresnelPower;
                half _ReflectionStrength;
                half4 _ReflectionTint;
            CBUFFER_END

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "SoF2/MapSurface"
}
