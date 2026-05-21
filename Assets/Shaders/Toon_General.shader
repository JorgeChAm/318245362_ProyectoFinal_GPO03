// Toon shader genérico para URP. Posteriza la iluminación en bandas duras
// estilo cartoon. Soporta directional + adicionales (Point/Spot), sombras,
// ambient via SH y rim glow.
//
// USO:
//   - Crear material con este shader (Custom → Toon General).
//   - Asignar al Mesh Renderer del personaje o escenario.
//   - Asignar la textura albedo del modelo a _MainTex.
//   - Ajustar _Bands según el look deseado.
Shader "Custom/Toon General"
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
        _RimThreshold ("Rim threshold (grosor del borde)", Range(0,1)) = 0.5
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

            // Sombras de la luz direccional principal.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // Luces adicionales (Point/Spot).
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // Forward+ rendering path (Unity 6 / URP 17).
            #pragma multi_compile _ _FORWARD_PLUS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Properties dentro del CBUFFER para que SRP Batcher las pueda agrupar.
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float  _Bands;
                float4 _ShadowTint;
                float  _AddLightScale;
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
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

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

            // Cuantiza un valor 0-1 en N pasos discretos (degradado smooth → bandas).
            float Posterize(float value, float steps)
            {
                return floor(value * steps) / max(steps - 1.0, 1.0);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                float3 normalWS = normalize(IN.normalWS);

                // Luz direccional principal + sombras.
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float NdotL    = saturate(dot(normalWS, mainLight.direction));
                float lighting = NdotL * mainLight.shadowAttenuation;
                float toonLight = Posterize(lighting, _Bands);

                // Ambient via spherical harmonics: la zona en sombra recoge el
                // rebote del skybox en vez de quedarse como tinte plano.
                half3 ambientLight = SampleSH(normalWS);
                half3 litColor     = albedo.rgb * mainLight.color;
                half3 shadowColor  = albedo.rgb * _ShadowTint.rgb * ambientLight;
                half3 finalColor   = lerp(shadowColor, litColor, toonLight);

                // Luces adicionales. InputData es requerido por el cluster
                // lookup de Forward+ y por el sampling de shadows.
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS   = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                uint pixelLightCount = GetAdditionalLightsCount();

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, IN.positionWS, inputData.shadowMask);

                    // Posterizar solo el ángulo (bandas duras de luz/sombra).
                    // La atenuación de distancia se aplica suave al final para
                    // que el falloff de la luz sea físicamente coherente.
                    float addNdotL  = saturate(dot(normalWS, addLight.direction));
                    float toonAngle = Posterize(addNdotL, _Bands);
                    float addAtt    = addLight.distanceAttenuation * addLight.shadowAttenuation;

                    finalColor += albedo.rgb * addLight.color * toonAngle * addAtt * _AddLightScale;
                LIGHT_LOOP_END

                // Rim glow en silueta. Posterizado para coherencia cartoon.
                // _RimColor.a = 0 desactiva el efecto.
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  rim     = 1.0 - saturate(dot(normalWS, viewDir));
                rim = pow(rim, _RimPower);
                rim = step(_RimThreshold, rim);
                finalColor += _RimColor.rgb * rim * _RimColor.a;

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // Permite que objetos con este shader proyecten sombras sobre otros.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}