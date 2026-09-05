Shader "VECTOR_CORE/CoreVisual"
{
    Properties
    {
        [Header(Core Colors)]
        [HDR]_CoreColor("Core Outline Color", Color) = (0.34,0.90,1,1)
        [HDR]_InnerColor("Inner Core Color", Color) = (0.90,0.98,1,1)
        _StructureColor("Middle Ring Color", Color) = (0.17,0.44,0.52,1)

        [Header(Core)]
        _CoreRadius("Core Radius", Range(0.1,0.4)) = 0.17
        _CoreOutlineRadius("Core Outline Radius", Range(0.2,0.5)) = 0.27
        _CoreOutlineWidth("Core Outline Width", Range(0.005,0.08)) = 0.022

        [Header(Middle Ring)]
        _MiddleRadius("Middle Radius", Range(0.3,0.7)) = 0.41
        _MiddleWidth("Middle Width", Range(0.005,0.1)) = 0.014

        [Header(Outer Segments)]
        [HDR]_OuterSegmentColor("Outer Segment Color", Color) = (0.42,0.93,1,1)
        _OuterRadius("Outer Radius", Range(0.5,0.95)) = 0.73
        _OuterWidth("Outer Width", Range(0.01,0.2)) = 0.082
        _OuterSegmentAngle("Outer Segment Angle", Range(15,80)) = 36

        [Header(Direction Marker)]
        [HDR]_MarkerColor("Marker Color", Color) = (0.42,0.93,1,1)
        _MarkerDistance("Marker Distance", Range(0.7,1.1)) = 0.87
        _ForwardMarkerSize("Forward Marker Size", Range(0.01,0.15)) = 0.045

        [Header(Element Slots)]
        _SlotRadius("Slot Radius", Range(0.4,0.8)) = 0.56
        _SlotWidth("Slot Fill Width", Range(0.01,0.15)) = 0.05
        _SlotBorderWidth("Slot Border Width", Range(0.02,0.2)) = 0.092
        _SlotAngle("Slot Arc Angle", Range(10,70)) = 24

        _EmptySlotColor("Empty Slot Color", Color) = (0.09,0.19,0.23,1)
        _SlotBorderColor("Slot Border Color", Color) = (0.18,0.42,0.49,1)

        [HDR]_SlotAColor("Slot A Color", Color) = (1,0.396,0.282,1)
        [HDR]_SlotBColor("Slot B Color", Color) = (1,0.831,0.278,1)

        _SlotAActive("Slot A Active", Range(0,1)) = 0
        _SlotBActive("Slot B Active", Range(0,1)) = 0

        [Header(Motion)]
        _Momentum("Momentum", Range(0,1)) = 0
        _ScanSpeed("Scan Speed", Float) = 0.8
        _ForwardDir("Forward Direction", Vector) = (0,1,0,0)

        [Header(Animation)]
        _CorePulseSpeed("Core Pulse Speed", Range(0,5)) = 1.6
        _CorePulseStrength("Core Pulse Strength", Range(0,0.15)) = 0.035

        _OuterPulseSpeed("Outer Pulse Speed", Range(0,5)) = 1.2
        _OuterPulseStrength("Outer Pulse Strength", Range(0,0.05)) = 0.008

        _SlotPulseSpeed("Slot Pulse Speed", Range(0,8)) = 3
        _SlotPulseStrength("Slot Pulse Strength", Range(0,0.5)) = 0.18

        [Header(Halo)]
        _HaloStrength("Halo Strength", Range(0,0.3)) = 0.06
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
                float4 _CoreColor;
                float4 _InnerColor;
                float4 _StructureColor;

                float4 _OuterSegmentColor;
                float4 _MarkerColor;

                float4 _EmptySlotColor;
                float4 _SlotBorderColor;
                float4 _SlotAColor;
                float4 _SlotBColor;

                float4 _ForwardDir;

                float _CoreRadius;
                float _CoreOutlineRadius;
                float _CoreOutlineWidth;

                float _MiddleRadius;
                float _MiddleWidth;

                float _OuterRadius;
                float _OuterWidth;
                float _OuterSegmentAngle;

                float _MarkerDistance;
                float _ForwardMarkerSize;

                float _SlotRadius;
                float _SlotWidth;
                float _SlotBorderWidth;
                float _SlotAngle;
                float _SlotAActive;
                float _SlotBActive;

                float _Momentum;
                float _ScanSpeed;

                float _CorePulseSpeed;
                float _CorePulseStrength;

                float _OuterPulseSpeed;
                float _OuterPulseStrength;

                float _SlotPulseSpeed;
                float _SlotPulseStrength;

                float _HaloStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float CircleAA(float radius, float targetRadius)
            {
                float aa = max(fwidth(radius), 0.001);
                return 1.0 - smoothstep(targetRadius, targetRadius + aa, radius);
            }

            float RingAA(float radius, float targetRadius, float width)
            {
                float distanceToRing = abs(radius - targetRadius);
                float aa = max(fwidth(radius), 0.001);
                return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, distanceToRing);
            }

            float AngleDistance(float a, float b)
            {
                return abs(atan2(sin(a - b), cos(a - b)));
            }

            float ArcMask(float angle, float centerAngle, float halfAngle)
            {
                float distanceToCenter = AngleDistance(angle, centerAngle);
                float aa = 0.025;
                return 1.0 - smoothstep(halfAngle, halfAngle + aa, distanceToCenter);
            }

            float TriangleMarkerAA(float2 position, float2 direction, float distanceFromCenter, float size)
            {
                float2 center = direction * distanceFromCenter;
                float2 q = position - center;
                float2 right = float2(direction.y, -direction.x);

                float localX = dot(q, right);
                float localY = dot(q, direction);

                float apexY = size;
                float baseY = -size * 0.55;

                float vertical01 = saturate((apexY - localY) / (apexY - baseY));
                float halfWidth = vertical01 * size * 0.72;

                float sideDistance = abs(localX) - halfWidth;
                float verticalDistance = max(localY - apexY, baseY - localY);
                float triangleDistance = max(sideDistance, verticalDistance);

                float aa = max(fwidth(triangleDistance), 0.001);
                return 1.0 - smoothstep(0.0, aa, triangleDistance);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float radius = length(p);

                float2 direction = _ForwardDir.xy;
                if (dot(direction, direction) < 0.0001) direction = float2(0,1);
                direction = normalize(direction);

                float angle = atan2(p.y, p.x);
                float directionAngle = atan2(direction.y, direction.x);
                float relativeAngle = atan2(sin(angle - directionAngle), cos(angle - directionAngle));

                // =========================================================
                // Animation
                // =========================================================

                float corePulse = sin(_Time.y * _CorePulseSpeed) * 0.5 + 0.5;
                float outerPulse = sin(_Time.y * _OuterPulseSpeed + 1.2) * 0.5 + 0.5;

                float slotPulseA = sin(_Time.y * _SlotPulseSpeed) * 0.5 + 0.5;
                float slotPulseB = sin(_Time.y * _SlotPulseSpeed + 2.4) * 0.5 + 0.5;

                // 高速状态下中心呼吸幅度减弱，让高速看起来更稳定
                float corePulseStrength = _CorePulseStrength * lerp(1.0, 0.45, _Momentum);

                // =========================================================
                // Inner Core
                // =========================================================

                float currentCoreRadius = lerp(_CoreRadius, _CoreRadius * 1.08, _Momentum);
                currentCoreRadius *= 1.0 + corePulse * corePulseStrength;

                float innerCore = CircleAA(radius, currentCoreRadius);
                float coreOutline = RingAA(radius, _CoreOutlineRadius, _CoreOutlineWidth);

                // =========================================================
                // Middle Ring
                // =========================================================

                float middleRing = RingAA(radius, _MiddleRadius, _MiddleWidth);

                float currentScanSpeed = _ScanSpeed * lerp(1.0, 2.2, _Momentum);
                float middleScan = sin(angle * 6.0 - _Time.y * currentScanSpeed) * 0.5 + 0.5;
                float middleBrightness = lerp(0.55, 0.95, middleScan);

                middleRing *= middleBrightness;

                // =========================================================
                // Four Equal Outer Segments
                // =========================================================

                float outerHalfAngle = _OuterSegmentAngle * 0.5 * PI / 180.0;

                float outerA = ArcMask(relativeAngle, PI * 0.25, outerHalfAngle);
                float outerB = ArcMask(relativeAngle, -PI * 0.25, outerHalfAngle);
                float outerC = ArcMask(relativeAngle, PI * 0.75, outerHalfAngle);
                float outerD = ArcMask(relativeAngle, -PI * 0.75, outerHalfAngle);

                float outerAngularMask = max(max(outerA, outerB), max(outerC, outerD));

                float currentOuterRadius = _OuterRadius + outerPulse * _OuterPulseStrength;
                float currentOuterWidth = lerp(_OuterWidth, _OuterWidth * 1.18, _Momentum);

                float outerSegments = RingAA(radius, currentOuterRadius, currentOuterWidth) * outerAngularMask;

                // =========================================================
                // Element Slots
                // =========================================================

                float slotHalfAngle = _SlotAngle * 0.5 * PI / 180.0;

                float slotAAngle = ArcMask(relativeAngle, PI * 0.5, slotHalfAngle);
                float slotBAngle = ArcMask(relativeAngle, -PI * 0.5, slotHalfAngle);

                float slotBorderRing = RingAA(radius, _SlotRadius, _SlotBorderWidth);
                float slotFillRing = RingAA(radius, _SlotRadius, _SlotWidth);

                float slotABorder = slotBorderRing * slotAAngle;
                float slotBBorder = slotBorderRing * slotBAngle;

                float slotAFill = slotFillRing * slotAAngle;
                float slotBFill = slotFillRing * slotBAngle;

                float3 slotAColor = lerp(_EmptySlotColor.rgb, _SlotAColor.rgb, _SlotAActive);
                float3 slotBColor = lerp(_EmptySlotColor.rgb, _SlotBColor.rgb, _SlotBActive);

                float slotAIntensity = lerp(0.55, 1.45 + slotPulseA * _SlotPulseStrength, _SlotAActive);
                float slotBIntensity = lerp(0.55, 1.45 + slotPulseB * _SlotPulseStrength, _SlotBActive);

                // =========================================================
                // Direction Marker
                // =========================================================

                float forwardMarker = TriangleMarkerAA(p, direction, _MarkerDistance, _ForwardMarkerSize);

                // =========================================================
                // Halo
                // =========================================================

                float halo = 1.0 - smoothstep(0.18, 0.65, radius);
                float haloPulse = lerp(0.88, 1.12, corePulse);

                halo *= (_HaloStrength + _Momentum * 0.07) * haloPulse;

                // =========================================================
                // Brightness
                // =========================================================

                float coreBrightness = lerp(1.05, 1.85, _Momentum);
                coreBrightness *= lerp(0.92, 1.08, corePulse);

                float outerBrightness = lerp(0.95, 1.35, _Momentum);
                float markerBrightness = lerp(1.0, 1.25, _Momentum);

                // =========================================================
                // Composition
                // =========================================================

                float3 color = 0;
                float alpha = 0;

                // Inner Core
                color += _InnerColor.rgb * innerCore * coreBrightness;
                alpha = max(alpha, innerCore);

                // Core Outline
                color += _CoreColor.rgb * coreOutline * 0.9;
                alpha = max(alpha, coreOutline * 0.9);

                // Middle Ring
                color += _StructureColor.rgb * middleRing;
                alpha = max(alpha, middleRing * 0.85);

                // Outer Segments
                color += _OuterSegmentColor.rgb * outerSegments * outerBrightness;
                alpha = max(alpha, outerSegments);

                // Slot Borders
                color += _SlotBorderColor.rgb * slotABorder * 0.85;
                color += _SlotBorderColor.rgb * slotBBorder * 0.85;

                alpha = max(alpha, slotABorder * 0.8);
                alpha = max(alpha, slotBBorder * 0.8);

                // Slot Fill
                color += slotAColor * slotAFill * slotAIntensity;
                color += slotBColor * slotBFill * slotBIntensity;

                alpha = max(alpha, slotAFill * lerp(0.55, 1.0, _SlotAActive));
                alpha = max(alpha, slotBFill * lerp(0.55, 1.0, _SlotBActive));

                // Triangle Marker
                color += _MarkerColor.rgb * forwardMarker * markerBrightness;
                alpha = max(alpha, forwardMarker);

                // Halo
                color += _CoreColor.rgb * halo;
                alpha = max(alpha, halo);

                return half4(color, saturate(alpha));
            }

            ENDHLSL
        }
    }
}