Shader "SoF2/Glass"
{
    Properties
    {
        _BaseMap ("Glass Texture", 2D) = "white" {}
        _BaseColor ("Glass Tint", Color) = (0.9, 0.95, 1.0, 0.3)
        _LightBlend ("Light Blend (0=Unlit, 1=Full Lit)", Range(0.0, 1.0)) = 0.5

        [Header(Transparency)]
        _Opacity ("Base Opacity", Range(0.0, 1.0)) = 0.25
        _DepthFadeRange ("Depth Fade Range", Range(0.01, 10.0)) = 1.0

        [Header(Fresnel Reflection)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 4.0
        _FresnelStrength ("Fresnel Strength", Range(0.0, 1.0)) = 0.6
        _ReflectionTint ("Reflection Tint", Color) = (0.8, 0.85, 0.95, 1.0)

        [Header(Specular)]
        _SpecularPower ("Specular Power", Range(8.0, 256.0)) = 96.0
        _SpecularIntensity ("Specular Intensity", Range(0.0, 3.0)) = 1.2

        _Cull ("Cull Mode", Float) = 0
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
        // SoF2 Glass Pass: Transparenz + Fresnel + Specular
        //
        // SoF2 Glass: blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        // Ergaenzt mit Fresnel-Reflektion (flacher Winkel = undurchsichtiger)
        // und Blinn-Phong Specular fuer Glas-typischen Glanz.
        // =====================================================
        Pass
        {
            Name "GlassForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
                half _Opacity;
                half _DepthFadeRange;
                half _FresnelPower;
                half _FresnelStrength;
                half4 _ReflectionTint;
                half _SpecularPower;
                half _SpecularIntensity;
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

                // === Fresnel: Flacher Winkel = stärkere Reflektion = undurchsichtiger ===
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelStrength;

                // === Beleuchtung (Lambert wie SoF2MapSurface) ===
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

                // Fresnel-Reflektion hinzufuegen
                color += fresnel * _ReflectionTint.rgb;

                // Specular-Glanz
                color += specularLight;

                // Alpha: Basis-Opacity + Fresnel macht Rand undurchsichtiger
                half alpha = _Opacity * _BaseColor.a * texColor.a;
                alpha = saturate(alpha + fresnel * 0.5);

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
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
                half _Opacity;
                half _DepthFadeRange;
                half _FresnelPower;
                half _FresnelStrength;
                half4 _ReflectionTint;
                half _SpecularPower;
                half _SpecularIntensity;
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

    FallBack "Universal Render Pipeline/Unlit"
}
