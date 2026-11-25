Shader "Universal Render Pipeline/Custom/SteampunkVertexLit"
{
    Properties
    {
        _Metallic("Metallic Sheen", Range(0, 1)) = 0.85
        _Smoothness("Smoothness / Gloss", Range(0, 1)) = 0.65
        _Ambient("Ambient Boost", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry" 
        }

        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float4 color        : COLOR;
                float3 positionWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Metallic;
                float _Smoothness;
                float _Ambient;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // Main directional light
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * (NdotL * (1.0 - _Metallic * 0.4));
                half3 ambient = SampleSH(normalWS) * 0.5 + half3(_Ambient, _Ambient, _Ambient) * 0.5;

                // Specular reflection
                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfDir));
                half specPower = _Smoothness * 60.0 + 4.0;
                half spec = pow(NdotH, specPower) * _Metallic;
                half3 specular = mainLight.color * spec;

                // Additional studio point lights
                uint pixelLightCount = GetAdditionalLightsCount();
                half3 addDiffuse = half3(0, 0, 0);
                half3 addSpec = half3(0, 0, 0);
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    half addNdotL = saturate(dot(normalWS, addLight.direction));
                    half atten = addLight.distanceAttenuation;
                    addDiffuse += addLight.color * (addNdotL * atten);

                    half3 addHalfDir = normalize(addLight.direction + viewDirWS);
                    half addNdotH = saturate(dot(normalWS, addHalfDir));
                    addSpec += addLight.color * (pow(addNdotH, specPower) * _Metallic * atten);
                }

                half3 baseCol = input.color.rgb;

                // Emission check for amber lamps
                half isLamp = step(0.9, baseCol.r) * step(0.55, baseCol.g) * (1.0 - step(0.35, baseCol.b));
                half3 emission = baseCol * (isLamp * 1.8);

                half3 finalColor = baseCol * (ambient + diffuse + addDiffuse) + specular + addSpec + emission;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
