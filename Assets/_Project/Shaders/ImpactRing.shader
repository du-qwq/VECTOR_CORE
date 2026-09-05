Shader "VECTOR_CORE/ImpactRing"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (0.35,0.9,1,1)
        _Progress("Progress", Range(0,1)) = 0
        _StartRadius("Start Radius", Range(0,1)) = 0.12
        _EndRadius("End Radius", Range(0,1.5)) = 0.82
        _RingWidth("Ring Width", Range(0.005,0.2)) = 0.045
        _Intensity("Intensity", Range(0,5)) = 1.5
        _SegmentStrength("Segment Strength", Range(0,1)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define PI 3.14159265359

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Progress;
                float _StartRadius;
                float _EndRadius;
                float _RingWidth;
                float _Intensity;
                float _SegmentStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float RingAA(float radius, float targetRadius, float width)
            {
                float d = abs(radius - targetRadius);
                float aa = max(fwidth(radius), 0.002);
                return 1.0 - smoothstep(width, width + aa, d);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);

                float easedProgress = 1.0 - pow(1.0 - _Progress, 2.0);

                float currentRadius = lerp(_StartRadius, _EndRadius, easedProgress);
                float currentWidth = lerp(_RingWidth * 1.35, _RingWidth * 0.55, _Progress);

                float ring = RingAA(radius, currentRadius, currentWidth);

                // 四方向轻微断续，让它不像普通圆圈
                float segmentPattern = abs(sin(angle * 4.0));
                float segmented = smoothstep(0.10, 0.30, segmentPattern);
                ring *= lerp(1.0, segmented, _SegmentStrength);

                // 第二层很弱的内侧回声环
                float echoProgress = saturate(_Progress * 1.35 - 0.15);
                float echoRadius = lerp(_StartRadius * 0.6, _EndRadius * 0.67, echoProgress);
                float echo = RingAA(radius, echoRadius, currentWidth * 0.45);
                echo *= (1.0 - echoProgress) * 0.42;

                float fade = pow(1.0 - _Progress, 1.7);

                float finalMask = ring + echo;
                float3 color = _Color.rgb * _Intensity * finalMask;
                float alpha = finalMask * fade * _Color.a;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}