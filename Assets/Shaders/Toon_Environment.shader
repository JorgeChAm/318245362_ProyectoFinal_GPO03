// Variante de Toon General para escenario (muros, piso, techo, props).
// La luz direccional principal se posteriza igual que en Toon General.
// Las luces adicionales (antorchas, hechizos) se aplican con gradiente SUAVE
// para evitar los círculos duros en las paredes.
Shader "Custom/Toon Environment"
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
        _AmbientBoost  ("Ambient boost (piso de brillo)", Range(0,1)) = 0.15

        [Header(Rim Light)]
        _RimColor     ("Rim color (alpha=0 desactiva)", Color) = (1,1,1,0)
        _RimPower     ("Rim power", Range(0.5, 8)) = 3
        _RimThreshold ("Rim threshold", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

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
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float  _Bands;
                float4 _ShadowTint;
                float  _AddLightScale;
                float  _AmbientBoost;
                float4 _RimColor;
                float  _RimPower;
                float  _RimThreshold;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
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
                UNITY_SETUP_INSTANCE_ID(IN);
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

                // ── Luz direccional principal: POSTERIZADA (look toon) ────────
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float NdotL     = saturate(dot(normalWS, mainLight.direction));
                float lighting  = NdotL * mainLight.shadowAttenuation;
                float toonLight = Posterize(lighting, _Bands);

                half3 ambientLight = SampleSH(normalWS);
                half3 litColor     = albedo.rgb * mainLight.color;
                half3 shadowColor  = albedo.rgb * _ShadowTint.rgb * (ambientLight + _AmbientBoost);
                half3 finalColor   = lerp(shadowColor, litColor, toonLight);

                // ── Luces adicionales: SUAVES (sin círculos duros) ────────────
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS   = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                uint pixelLightCount = GetAdditionalLightsCount();

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, IN.positionWS, inputData.shadowMask);

                    // NdotL suave + smoothstep en la atenuación de distancia
                    // para que el borde de la antorcha se difumine gradualmente.
                    float addNdotL = saturate(dot(normalWS, addLight.direction));
                    float addAtt   = addLight.distanceAttenuation * addLight.shadowAttenuation;

                    finalColor += albedo.rgb * addLight.color * addNdotL * addAtt * _AddLightScale;
                LIGHT_LOOP_END

                // ── Rim: desactivado por defecto en escenario ─────────────────
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  rim     = 1.0 - saturate(dot(normalWS, viewDir));
                rim = pow(rim, _RimPower);
                rim = step(_RimThreshold, rim);
                finalColor += _RimColor.rgb * rim * _RimColor.a;

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}
