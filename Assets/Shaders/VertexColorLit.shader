Shader "Universal Render Pipeline/Custom/VertexColorLit"
{
    Properties
    {
        _GroundColor("Ground Meadow Color", Color) = (0.36, 0.65, 0.32, 1)
        _AmbientBoost("Ambient Boost", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry" 
        }

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
                float4 _GroundColor;
                float _AmbientBoost;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;

                // Заменяем белый/серый непокрашенный рельеф из Houdini на сочный цвет лесной поляны
                half isWhite = step(0.92, input.color.r) * step(0.92, input.color.g) * step(0.92, input.color.b);
                half3 col = lerp(input.color.rgb, _GroundColor.rgb, isWhite);

                output.color = float4(col, 1.0);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 direct = mainLight.color * NdotL;
                
                half3 lightTotal = max(ambient + direct, half3(_AmbientBoost, _AmbientBoost, _AmbientBoost));
                half3 finalColor = input.color.rgb * lightTotal;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
