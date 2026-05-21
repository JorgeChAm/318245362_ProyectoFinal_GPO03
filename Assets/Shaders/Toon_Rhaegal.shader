// Variante de Toon General exclusiva para Rhaegal.
// Idéntica en iluminación + segunda pasada de Inverted Hull para el aura
// de silueta. _OutlineColor.a = 0 desactiva el aura sin cambiar material.
Shader "Custom/Toon Rhaegal"
{
    Properties
    {
        [Header(Base)]
        _BaseColor   ("Color base", Color) = (1,1,1,1)
        _MainTex     ("Albedo (texture)", 2D) = "white" {}

        [Header(Toon Lighting)]
        _Bands       ("Numero de bandas", Range(2,5)) = 3
        _ShadowTint  ("Color de sombra", Color) = (0.25, 0.25, 0.35, 1)

        [Header(Additional Lights)]
        _AddLightScale ("Escala luces adicionales", Range(0,1)) = 1.0

        [Header(Rim Light)]
        _RimColor     ("Rim color (alpha=0 desactiva)", Color) = (1,1,1,0.5)
        _RimPower     ("Rim power", Range(0.5, 8)) = 3
        _RimThreshold ("Rim threshold", Range(0,1)) = 0.5

        [Header(Aura Silueta)]
        [HDR] _OutlineColor ("Aura color (alpha=0 desactiva)", Color) = (0.78, 0.31, 0.78, 1)
        _OutlineWidth       ("Aura grosor", Range(0, 0.1)) = 0.03

        [Header(Emision Interna)]
        [HDR] _EmissionColor ("Emision color (alpha=0 desactiva)", Color) = (0.78, 0.31, 0.78, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // ── PASS 1: Iluminación toon (idéntica a Toon General) ────────────────
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float  _Bands;
                float4 _ShadowTint;
                float  _AddLightScale;
                float4 _RimColor;
                float  _RimPower;
                float  _RimThreshold;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float4 _EmissionColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            float Posterize(float value, float steps)
            {
                return floor(value * steps) / max(steps - 1.0, 1.0);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = nrmInputs.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;
                float3 normalWS = normalize(IN.normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float NdotL    = saturate(dot(normalWS, mainLight.direction));
                float lighting  = NdotL * mainLight.shadowAttenuation;
                float toonLight = Posterize(lighting, _Bands);

                half3 ambientLight = SampleSH(normalWS);
                half3 litColor     = albedo.rgb * mainLight.color;
                half3 shadowColor  = albedo.rgb * _ShadowTint.rgb * ambientLight;
                half3 finalColor   = lerp(shadowColor, litColor, toonLight);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS   = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight  = GetAdditionalLight(lightIndex, IN.positionWS, inputData.shadowMask);
                    float addNdotL  = saturate(dot(normalWS, addLight.direction));
                    float toonAngle = Posterize(addNdotL, _Bands);
                    float addAtt    = addLight.distanceAttenuation * addLight.shadowAttenuation;
                    finalColor += albedo.rgb * addLight.color * toonAngle * addAtt * _AddLightScale;
                LIGHT_LOOP_END

                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  rim     = 1.0 - saturate(dot(normalWS, viewDir));
                rim = pow(rim, _RimPower);
                rim = step(_RimThreshold, rim);
                finalColor += _RimColor.rgb * rim * _RimColor.a;

                // Emisión interna — brillo sayayin desde el cuerpo hacia afuera
                finalColor += _EmissionColor.rgb * _EmissionColor.a;

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // ── PASS 2: Inverted Hull — aura de silueta ───────────────────────────
        Pass
        {
            Name "Outline"
            Cull Front      // solo caras traseras → silueta exterior
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   OutlineVert
            #pragma fragment OutlineFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float  _Bands;
                float4 _ShadowTint;
                float  _AddLightScale;
                float4 _RimColor;
                float  _RimPower;
                float  _RimThreshold;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                // Extruir vértices a lo largo de la normal en Object Space
                float3 extruded = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionCS  = TransformObjectToHClip(extruded);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                // Alpha = 0 desactiva el aura sin borrar el material
                clip(_OutlineColor.a - 0.01);
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}
