Shader "VECTOR_CORE/ArenaModuleLine"
{
    Properties
    {
        [HDR]_BaseColor("Base Color", Color) = (0.17,0.26,0.32,1)
        [HDR]_FlowColor("Flow Color", Color) = (0.40,0.91,1,1)

        _BaseAlpha("Base Alpha", Range(0,1)) = 0.5

        _FlowStrength("Flow Strength", Range(0,5)) = 1.5
        _FlowSpeed("Flow Speed", Range(-3,3)) = 0.25
        _FlowWidth("Flow Width", Range(0.01,0.5)) = 0.10
        _FlowSoftness("Flow Softness", Range(0.001,0.3)) = 0.06
        _FlowOffset("Flow Offset", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FlowColor;

                float _BaseAlpha;
                float _FlowStrength;
                float _FlowSpeed;
                float _FlowWidth;
                float _FlowSoftness;
                float _FlowOffset;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float across = 1.0 - abs(input.uv.y * 2.0 - 1.0);
                across = pow(saturate(across), 1.4);

                float phase = frac(input.uv.x - _Time.y * _FlowSpeed + _FlowOffset);
                float distanceToBand = abs(phase - 0.5);

                float flow = 1.0 - smoothstep(
                    _FlowWidth,
                    _FlowWidth + _FlowSoftness,
                    distanceToBand
                );

                float3 color = _BaseColor.rgb;
                color += _FlowColor.rgb * flow * _FlowStrength;

                float alpha = _BaseAlpha;
                alpha += flow * 0.65;
                alpha *= across;
                alpha *= input.color.a;

                return half4(color * input.color.rgb, saturate(alpha));
            }

            ENDHLSL
        }
    }
}