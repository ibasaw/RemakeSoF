Shader "SoF2/Ice"
{
    Properties
    {
        _BaseMap ("Ice Texture", 2D) = "white" {}
        _BaseColor ("Ice Tint", Color) = (0.8, 0.9, 1.0, 0.85)
        _LightBlend ("Light Blend (0=Unlit, 1=Full Lit)", Range(0.0, 1.0)) = 0.6

        [Header(Transparency)]
        _Opacity ("Base Opacity", Range(0.0, 1.0)) = 0.8
        _DepthFadeRange ("Depth Fade Range", Range(0.01, 10.0)) = 2.0

        [Header(Fresnel Reflection)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0.0, 1.0)) = 0.5
        _ReflectionTint ("Reflection Tint", Color) = (0.7, 0.85, 1.0, 1.0)

        [Header(Specular Gloss)]
        _SpecularPower ("Specular Sharpness", Range(8.0, 512.0)) = 128.0
        _SpecularIntensity ("Specular Intensity", Range(0.0, 5.0)) = 2.0

        [Header(Sub Surface Distortion)]
        _SubSurfaceStrength ("Sub-Surface Strength", Range(0.0, 1.0)) = 0.15
        _SubSurfaceColor ("Sub-Surface Color", Color) = (0.5, 0.7, 0.9, 1.0)

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
        // SoF2 Ice Pass: Halbtransparent + Stark Specular + Fresnel
        //
        // Eis ist halbtransparent mit hoher Spiegelung.
        // Starkes Specular-Highlight simuliert glatte Oberflaeche.
        // Sub-Surface Scattering (vereinfacht): Licht scheint
        // durch duennere Stellen leicht blaeulich durch.
        // =====================================================
        Pass
        {
            Name "IceForward"
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float4 projPos : TEXCOORD5;
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
                half _SubSurfaceStrength;
                half4 _SubSurfaceColor;
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

                output.projPos = ComputeScreenPos(output.positionCS);
                output.projPos.z = -TransformWorldToView(vertexInput.positionWS).z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // === Fresnel: Eis reflektiert stark bei flachem Winkel ===
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelStrength;

                // === Beleuchtung ===
                half3 totalLight = half3(0, 0, 0);
                half3 specularLight = half3(0, 0, 0);
                half3 subSurface = half3(0, 0, 0);

                if (_LightBlend > 0.001)
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    half NdotL = saturate(dot(normalWS, mainLight.direction));
                    half3 mainLighting = mainLight.color * NdotL
                        * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                    // Blinn-Phong Specular (sehr scharf fuer Eis)
                    float3 halfDir = normalize(mainLight.direction + viewDir);
                    float NdotH = saturate(dot(normalWS, halfDir));
                    specularLight += mainLight.color * pow(NdotH, _SpecularPower)
                        * _SpecularIntensity * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                    // Sub-Surface: Licht das durch Eis scheint (wrap-around lighting)
                    float wrapNdotL = saturate(dot(normalWS, mainLight.direction) * 0.5 + 0.5);
                    subSurface += _SubSurfaceColor.rgb * mainLight.color * wrapNdotL
                        * _SubSurfaceStrength * mainLight.distanceAttenuation;

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

                // === Depth-Fade: duennere Stellen transparenter ===
                float2 screenUV = input.projPos.xy / input.projPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragDepth = input.projPos.z;
                float depthDiff = sceneDepth - fragDepth;
                float depthFade = saturate(depthDiff / _DepthFadeRange);

                // === Zusammenbauen ===
                half baseDarkening = 1.0 - _LightBlend;
                half3 litFactor = baseDarkening + _LightBlend * totalLight;

                half3 color = texColor.rgb * _BaseColor.rgb * litFactor;

                // Sub-Surface Durchschein-Effekt
                color += subSurface;

                // Fresnel-Reflektion
                color += fresnel * _ReflectionTint.rgb;

                // Specular auf Eis-Oberflaeche
                color += specularLight;

                // Alpha
                half alpha = _Opacity * _BaseColor.a * texColor.a * depthFade;
                alpha = saturate(alpha + fresnel * 0.4);

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
                half _SubSurfaceStrength;
                half4 _SubSurfaceColor;
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
