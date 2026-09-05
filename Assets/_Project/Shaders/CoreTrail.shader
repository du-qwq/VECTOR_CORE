Shader "VECTOR_CORE/CoreTrail"
{
    Properties
    {
        [HDR]_Tint("Tint", Color) = (0.19,0.84,1,1)

        _Intensity("Intensity", Range(0,5)) = 1
        _Opacity("Opacity", Range(0,1)) = 1

        _CenterPower("Center Power", Range(0.2,8)) = 1.5
        _TailFadePower("Tail Fade Power", Range(0.2,5)) = 1

        _Boost("Boost", Range(0,1)) = 0
        _BoostIntensity("Boost Intensity", Range(0,4)) = 1
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
                float4 _Tint;

                float _Intensity;
                float _Opacity;

                float _CenterPower;
                float _TailFadePower;

                float _Boost;
                float _BoostIntensity;
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
                // 横截面：中心最亮，两侧柔和消失
                float across = 1.0 - abs(input.uv.y * 2.0 - 1.0);
                across = pow(saturate(across), _CenterPower);

                // 沿 Trail 长度方向渐隐
                float along = pow(saturate(input.uv.x), _TailFadePower);

                float boostBrightness = 1.0 + _Boost * _BoostIntensity;

                float alpha = input.color.a;
                alpha *= _Tint.a;
                alpha *= _Opacity;
                alpha *= across;
                alpha *= along;

                float3 color = input.color.rgb;
                color *= _Tint.rgb;
                color *= _Intensity;
                color *= boostBrightness;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}