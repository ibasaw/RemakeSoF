Shader "SoF2/Water"
{
    Properties
    {
        _BaseMap ("Water Texture", 2D) = "white" {}
        _BaseColor ("Water Tint", Color) = (0.3, 0.5, 0.6, 0.65)
        _LightBlend ("Light Blend (0=Unlit, 1=Full Lit)", Range(0.0, 1.0)) = 0.5

        [Header(Wave Animation)]
        _WaveAmplitude ("Wave Amplitude", Range(0.0, 0.5)) = 0.04
        _WaveFrequency ("Wave Frequency", Range(0.1, 10.0)) = 1.5
        _WaveSpeed ("Wave Speed", Range(0.1, 5.0)) = 0.8

        [Header(Texture Scroll)]
        _ScrollSpeedX ("Scroll Speed X", Range(-1.0, 1.0)) = 0.07
        _ScrollSpeedY ("Scroll Speed Y", Range(-1.0, 1.0)) = -0.03
        _TurbAmplitude ("Turbulence Strength", Range(0.0, 0.2)) = 0.03
        _TurbFrequency ("Turbulence Frequency", Range(0.1, 5.0)) = 0.25
        _UVScale ("UV Scale", Range(0.1, 2.0)) = 0.33

        [Header(Depth Fade)]
        _DepthFadeRange ("Depth Fade Range", Range(0.1, 20.0)) = 3.0
        _ShallowColor ("Shallow Tint", Color) = (0.4, 0.7, 0.8, 0.4)
        _DeepColor ("Deep Tint", Color) = (0.1, 0.2, 0.3, 0.85)

        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 3.0
        _FresnelBoost ("Fresnel Boost", Range(0.0, 1.0)) = 0.3

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
        // SoF2 Water Pass: Authentische idTech3-Wasseroptik
        //
        // Basierend auf den SoF2 .g2shader Parametern:
        //   tcMod turb  0.25 0.03 0.025 0.25  → UV-Turbulenz
        //   tcMod scroll 0.07 -0.03            → UV-Verschiebung
        //   tcMod scale  0.33 0.33             → UV-Skalierung
        //   blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        //
        // Erweitert mit:
        //   - Vertex-Wellen (sin-Displacement auf Y-Achse)
        //   - Depth-Fade (seichtes Wasser heller, tiefes dunkler)
        //   - Fresnel (steiler Winkel = reflektiver)
        //   - Lambert-Beleuchtung wie SoF2MapSurface
        // =====================================================
        Pass
        {
            Name "WaterForward"
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
                float fogFactor : TEXCOORD3;
                float4 projPos : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _LightBlend;

                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveSpeed;

                half _ScrollSpeedX;
                half _ScrollSpeedY;
                half _TurbAmplitude;
                half _TurbFrequency;
                half _UVScale;

                half _DepthFadeRange;
                half4 _ShallowColor;
                half4 _DeepColor;

                half _FresnelPower;
                half _FresnelBoost;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS.xyz;

                // Vertex-Wellen: zwei ueberlagerte Sinuswellen auf Y-Achse.
                // Jeder Vertex bekommt eine individuelle Phase basierend auf seiner XZ-Position.
                // SoF2 hat keine Vertex-Deformation im Shader, aber die tcMod turb Werte
                // erzeugen den gleichen visuellen Effekt auf UV-Ebene.
                // Wir ergaenzen leichte geometrische Wellen fuer mehr Tiefe.
                float phase1 = posOS.x * _WaveFrequency + _Time.y * _WaveSpeed;
                float phase2 = posOS.z * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3;
                posOS.y += sin(phase1) * _WaveAmplitude + cos(phase2) * _WaveAmplitude * 0.5;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                // Screen-Position fuer Depth-Fade
                output.projPos = ComputeScreenPos(output.positionCS);
                output.projPos.z = -TransformWorldToView(vertexInput.positionWS).z;

                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // === UV Animation (SoF2: tcMod scale + turb + scroll) ===
                float2 uv = input.uv * _UVScale;

                // tcMod scroll: Konstante UV-Verschiebung ueber Zeit
                uv += float2(_ScrollSpeedX, _ScrollSpeedY) * _Time.y;

                // tcMod turb: Sinuswellen-Verzerrung auf UV-Koordinaten
                // SoF2-Formel: turb(amplitude, phase, frequency, time)
                float turbPhase = _Time.y * _TurbFrequency;
                uv.x += sin(uv.y * 3.14159 * 2.0 + turbPhase) * _TurbAmplitude;
                uv.y += cos(uv.x * 3.14159 * 2.0 + turbPhase * 1.1) * _TurbAmplitude;

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);

                // === Depth-Fade: seichtes Wasser heller/transparenter, tiefes dunkler ===
                float2 screenUV = input.projPos.xy / input.projPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float waterDepth = sceneDepth - input.projPos.z;
                float depthFactor = saturate(waterDepth / _DepthFadeRange);

                // Farbe: Interpolation von Shallow zu Deep basierend auf Tiefe
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                // === Fresnel: Flacher Blickwinkel = reflektiver/heller ===
                float3 normalWS = normalize(input.normalWS);
                float NdotV = saturate(dot(normalWS, input.viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelBoost;

                // === Beleuchtung (wie SoF2MapSurface: Lambert + Additional) ===
                half3 totalLight = half3(0, 0, 0);

                if (_LightBlend > 0.001)
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    half NdotL = saturate(dot(normalWS, mainLight.direction));
                    half3 mainLighting = mainLight.color * NdotL
                        * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

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
                        LIGHT_LOOP_END
                    #endif

                    half3 ambient = SampleSH(normalWS);
                    totalLight = min(mainLighting + additionalLighting + ambient, 5.0);
                }

                // === Zusammenbauen ===
                half baseDarkening = 1.0 - _LightBlend;
                half3 litFactor = baseDarkening + _LightBlend * totalLight;

                // Textur × WasserTint × Beleuchtung
                half3 color = texColor.rgb * waterColor.rgb * _BaseColor.rgb * litFactor;

                // Fresnel: Aufhellen bei flachem Winkel (Reflektion simulieren)
                color += fresnel * waterColor.rgb * 0.5;

                // Alpha: Tiefenabhaengig + Basis-Transparenz
                half alpha = waterColor.a * _BaseColor.a;
                // Fresnel macht Oberflaeche bei flachem Winkel weniger durchsichtig
                alpha = saturate(alpha + fresnel * 0.3);

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }

        // =====================================================
        // Depth Only Pass: fuer ZPrepass / Depth-Korrektheit
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
                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveSpeed;
                half _ScrollSpeedX;
                half _ScrollSpeedY;
                half _TurbAmplitude;
                half _TurbFrequency;
                half _UVScale;
                half _DepthFadeRange;
                half4 _ShallowColor;
                half4 _DeepColor;
                half _FresnelPower;
                half _FresnelBoost;
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

                float3 posOS = input.positionOS.xyz;
                float phase1 = posOS.x * _WaveFrequency + _Time.y * _WaveSpeed;
                float phase2 = posOS.z * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3;
                posOS.y += sin(phase1) * _WaveAmplitude + cos(phase2) * _WaveAmplitude * 0.5;

                output.positionCS = TransformObjectToHClip(posOS);
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
