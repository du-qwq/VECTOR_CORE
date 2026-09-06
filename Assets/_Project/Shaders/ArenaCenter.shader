Shader "VECTOR_CORE/ArenaCenter"
{
    Properties
    {
        [Header(Color)]
        _StructureColor("Structure Color", Color) = (0.17,0.26,0.32,0.55)
        [HDR]_SignalColor("Signal Color", Color) = (0.40,0.91,1.00,1)

        [Header(Main Ring)]
        _RingRadius("Ring Radius", Range(0.5,3)) = 1.55
        _RingWidth("Ring Width", Range(0.005,0.1)) = 0.018
        _RingAlpha("Ring Alpha", Range(0,1)) = 0.38

        [Header(Broken Arcs)]
        _ArcRadius("Arc Radius", Range(0.5,3)) = 2.05
        _ArcWidth("Arc Width", Range(0.005,0.15)) = 0.035
        _ArcLength("Arc Length", Range(0.05,0.24)) = 0.115
        _ArcAlpha("Arc Alpha", Range(0,1)) = 0.50
        _RotationSpeed("Rotation Speed", Range(-0.1,0.1)) = 0.012

        [Header(Flow Signal)]
        _SignalLength("Signal Length", Range(0.005,0.12)) = 0.035
        _SignalSoftness("Signal Softness", Range(0.001,0.05)) = 0.012
        _SignalSpeed("Signal Speed", Range(-0.3,0.3)) = 0.045
        _SignalStrength("Signal Strength", Range(0,3)) = 1.20

        [Header(Center Locator)]
        _LocatorRadius("Locator Radius", Range(0.02,0.5)) = 0.16
        _LocatorWidth("Locator Width", Range(0.005,0.1)) = 0.025
        _LocatorDotRadius("Locator Dot Radius", Range(0.005,0.15)) = 0.035
        _LocatorAlpha("Locator Alpha", Range(0,1)) = 0.65
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _StructureColor;
                float4 _SignalColor;
                float _RingRadius;
                float _RingWidth;
                float _RingAlpha;
                float _ArcRadius;
                float _ArcWidth;
                float _ArcLength;
                float _ArcAlpha;
                float _RotationSpeed;
                float _SignalLength;
                float _SignalSoftness;
                float _SignalSpeed;
                float _SignalStrength;
                float _LocatorRadius;
                float _LocatorWidth;
                float _LocatorDotRadius;
                float _LocatorAlpha;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float RingAA(float distanceFromCenter, float radius, float width)
            {
                float d = abs(distanceFromCenter - radius);
                float aa = max(fwidth(d), 0.0005);
                return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, d);
            }

            float CircleAA(float distanceFromCenter, float radius)
            {
                float aa = max(fwidth(distanceFromCenter), 0.0005);
                return 1.0 - smoothstep(radius, radius + aa, distanceFromCenter);
            }

            float WrappedDistance(float a, float b)
            {
                float d = abs(a - b);
                return min(d, 1.0 - d);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 6.0;
                float radius = length(p);
                float angle = frac(atan2(p.y, p.x) / 6.2831853 + 1.0);

                float3 color = 0;
                float alpha = 0;

                float mainRing = RingAA(radius, _RingRadius, _RingWidth);
                color += _StructureColor.rgb * mainRing * _RingAlpha;
                alpha = max(alpha, mainRing * _RingAlpha);

                float rotatingAngle = frac(angle - _Time.y * _RotationSpeed);
                float quadrant = frac(rotatingAngle * 4.0);
                float arcDistance = abs(quadrant - 0.5);
                float arcAngularMask = 1.0 - smoothstep(_ArcLength, _ArcLength + 0.015, arcDistance);
                float arcRadialMask = RingAA(radius, _ArcRadius, _ArcWidth);
                float arcs = arcAngularMask * arcRadialMask;

                color += _StructureColor.rgb * arcs * _ArcAlpha;
                alpha = max(alpha, arcs * _ArcAlpha);

                float signalPhase = frac(angle - _Time.y * _SignalSpeed);
                float signalCenter = 0.125;
                float signalDistance = WrappedDistance(signalPhase, signalCenter);
                float signalAngular = 1.0 - smoothstep(_SignalLength, _SignalLength + _SignalSoftness, signalDistance);
                float signal = signalAngular * arcRadialMask;

                color += _SignalColor.rgb * signal * _SignalStrength;
                alpha = max(alpha, signal * _SignalColor.a);

                float locatorRing = RingAA(radius, _LocatorRadius, _LocatorWidth);
                float locatorDot = CircleAA(radius, _LocatorDotRadius);
                float locator = max(locatorRing, locatorDot);

                color += _SignalColor.rgb * locator * _LocatorAlpha;
                alpha = max(alpha, locator * _LocatorAlpha);

                return half4(color, saturate(alpha));
            }

            ENDHLSL
        }
    }
}