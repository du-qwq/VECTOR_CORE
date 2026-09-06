Shader "VECTOR_CORE/ArenaModuleLine"
{
    Properties
    {
        [HDR]_BaseColor("Base Color", Color) = (0.15,0.38,0.44,1)
        [HDR]_FlowColor("Flow Color", Color) = (0.40,0.91,1,1)

        _BaseAlpha("Base Alpha", Range(0,1)) = 0.30

        _FlowStrength("Flow Strength", Range(0,5)) = 1.8
        _FlowSpeed("Flow Speed", Range(-3,3)) = 0.22
        _FlowWidth("Flow Width", Range(0.01,0.5)) = 0.10
        _FlowSoftness("Flow Softness", Range(0.001,0.3)) = 0.055
        _FlowOffset("Flow Offset", Range(0,1)) = 0

        _CoreWidth("Flow Core Width", Range(0.01,0.3)) = 0.035
        _CoreStrength("Flow Core Strength", Range(0,5)) = 1.4
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

                float _CoreWidth;
                float _CoreStrength;
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
                across = pow(saturate(across), 1.6);

                float phase = frac(input.uv.x - _Time.y * _FlowSpeed + _FlowOffset);
                float distanceToFlow = abs(phase - 0.5);

                float glow = 1.0 - smoothstep(_FlowWidth, _FlowWidth + _FlowSoftness, distanceToFlow);
                float core = 1.0 - smoothstep(_CoreWidth, _CoreWidth + _FlowSoftness * 0.45, distanceToFlow);

                float3 color = _BaseColor.rgb;
                color += _FlowColor.rgb * glow * _FlowStrength;
                color += _FlowColor.rgb * core * _CoreStrength;

                float alpha = _BaseAlpha;
                alpha += glow * 0.42;
                alpha += core * 0.35;
                alpha *= across;
                alpha *= input.color.a;

                return half4(color * input.color.rgb, saturate(alpha));
            }

            ENDHLSL
        }
    }
}