Shader "VECTOR_CORE/ImpactLine"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (0.22,0.68,0.9,1)
        _Progress("Progress", Range(0,1)) = 0

        _Length("Length", Range(0.1,1)) = 0.8
        _Thickness("Thickness", Range(0.005,0.5)) = 0.10

        _Intensity("Intensity", Range(0,5)) = 1.8
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
                float _Length;
                float _Thickness;
                float _Intensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;

                float eased = 1.0 - pow(1.0 - _Progress, 3.0);

                float currentLength = lerp(0.12, _Length, eased);
                float currentThickness = lerp(_Thickness * 1.6, _Thickness * 0.55, _Progress);

                float horizontal = 1.0 - smoothstep(currentLength, currentLength + 0.03, abs(p.x));
                float vertical = 1.0 - smoothstep(currentThickness, currentThickness + 0.025, abs(p.y));

                float endFade = 1.0 - pow(saturate(abs(p.x) / max(currentLength, 0.001)), 2.5);

                float mask = horizontal * vertical * endFade;

                float hotCore = 1.0 - smoothstep(currentThickness * 0.25, currentThickness * 0.65, abs(p.y));
                float brightness = 1.0 + hotCore * 1.2;

                float fade = pow(1.0 - _Progress, 1.8);

                float3 color = _Color.rgb * _Intensity * brightness * mask;
                float alpha = mask * fade * _Color.a;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}