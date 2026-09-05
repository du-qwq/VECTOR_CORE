Shader "VECTOR_CORE/CoreVisual"
{
    Properties
    {
        [Header(Core Colors)]
        [HDR]_CoreColor("Core Outline Color", Color) = (0.34,0.90,1,1)
        [HDR]_InnerColor("Inner Core Color", Color) = (0.90,0.98,1,1)
        _StructureColor("Structure Color", Color) = (0.17,0.44,0.52,1)

        [Header(Core)]
        _CoreRadius("Core Radius", Range(0.1,0.4)) = 0.22
        _CoreOutlineRadius("Core Outline Radius", Range(0.2,0.5)) = 0.31
        _CoreOutlineWidth("Core Outline Width", Range(0.005,0.08)) = 0.022

        [Header(Inner Rotor)]
        [HDR]_RotorColor("Rotor Color", Color) = (0.25,0.78,0.92,1)
        _RotorRadius("Rotor Radius", Range(0.3,0.55)) = 0.39
        _RotorWidth("Rotor Width", Range(0.01,0.10)) = 0.045
        _RotorSegmentAngle("Rotor Segment Angle", Range(10,80)) = 34
        [HideInInspector]_RotorRotationPhase("Rotor Rotation Phase", Float) = 0

        [Header(Middle Ring)]
        _MiddleRadius("Middle Radius", Range(0.3,0.7)) = 0.50
        _MiddleWidth("Middle Width", Range(0.005,0.1)) = 0.012
        _MiddleScanAngle("Middle Scan Arc", Range(5,90)) = 28

        [Header(Outer Segments)]
        [HDR]_OuterSegmentColor("Outer Segment Color", Color) = (0.42,0.93,1,1)
        _OuterRadius("Outer Radius", Range(0.5,0.95)) = 0.78
        _OuterWidth("Outer Width", Range(0.01,0.2)) = 0.10
        _OuterSegmentAngle("Outer Segment Angle", Range(15,80)) = 42
        _OuterFlowSpeed("Outer Flow Speed", Range(0,8)) = 2.2
        _OuterFlowStrength("Outer Flow Strength", Range(0,1)) = 0.35

        [Header(Direction Marker)]
        [HDR]_MarkerColor("Marker Color", Color) = (0.42,0.93,1,1)
        _MarkerDistance("Marker Distance", Range(0.7,1.1)) = 0.87
        _ForwardMarkerSize("Forward Marker Size", Range(0.01,0.15)) = 0.055

        [Header(Element Slots)]
        _SlotRadius("Slot Radius", Range(0.4,0.8)) = 0.58
        _SlotWidth("Slot Fill Width", Range(0.01,0.15)) = 0.06
        _SlotBorderWidth("Slot Border Width", Range(0.02,0.2)) = 0.125
        _SlotAngle("Slot Arc Angle", Range(10,70)) = 34
        _SlotEndBorderAngle("Slot End Border Angle", Range(0.5,10)) = 3.5

        _EmptySlotColor("Empty Slot Color", Color) = (0.027,0.071,0.090,1)
        _SlotBorderColor("Slot Border Color", Color) = (0.18,0.40,0.48,1)

        [HDR]_SlotAColor("Slot A Color", Color) = (1,0.396,0.282,1)
        [HDR]_SlotBColor("Slot B Color", Color) = (1,0.831,0.278,1)

        _SlotAActive("Slot A Active", Range(0,1)) = 0
        _SlotBActive("Slot B Active", Range(0,1)) = 0

        [Header(Element Slot Flow)]
        _SlotScanSpeed("Slot Scan Speed", Range(0,5)) = 1.6
        _SlotScanWidth("Slot Scan Width", Range(0.05,0.8)) = 0.22
        _SlotScanStrength("Slot Scan Strength", Range(0,3)) = 1.8

        [Header(Motion)]
        _Momentum("Momentum", Range(0,1)) = 0
        _ScanSpeed("Middle Scan Speed", Range(0,6)) = 1.2
        _ForwardDir("Forward Direction", Vector) = (0,1,0,0)

        [Header(Boost)]
        _BoostFlash("Boost Flash", Range(0,1)) = 0
        _BoostWave("Boost Wave", Range(-1,1)) = 0
        _BoostOuterOffset("Boost Outer Offset", Range(0,0.15)) = 0.055

        [Header(Animation)]
        _CorePulseSpeed("Core Pulse Speed", Range(0,8)) = 2
        _CorePulseStrength("Core Pulse Strength", Range(0,0.15)) = 0.045
        _SlotPulseSpeed("Slot Pulse Speed", Range(0,8)) = 3
        _SlotPulseStrength("Slot Pulse Strength", Range(0,0.5)) = 0.12

        [Header(Halo)]
        _HaloStrength("Halo Strength", Range(0,0.3)) = 0.055
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

                float4 _RotorColor;
                float _RotorRadius;
                float _RotorWidth;
                float _RotorSegmentAngle;
                float _RotorRotationPhase;

                float _MiddleRadius;
                float _MiddleWidth;
                float _MiddleScanAngle;

                float4 _OuterSegmentColor;
                float _OuterRadius;
                float _OuterWidth;
                float _OuterSegmentAngle;
                float _OuterFlowSpeed;
                float _OuterFlowStrength;

                float4 _MarkerColor;
                float _MarkerDistance;
                float _ForwardMarkerSize;

                float4 _EmptySlotColor;
                float4 _SlotBorderColor;
                float4 _SlotAColor;
                float4 _SlotBColor;

                float _SlotRadius;
                float _SlotWidth;
                float _SlotBorderWidth;
                float _SlotAngle;
                float _SlotEndBorderAngle;

                float _SlotAActive;
                float _SlotBActive;

                float _SlotScanSpeed;
                float _SlotScanWidth;
                float _SlotScanStrength;

                float4 _ForwardDir;

                float _CoreRadius;
                float _CoreOutlineRadius;
                float _CoreOutlineWidth;

                float _Momentum;
                float _ScanSpeed;

                float _BoostFlash;
                float _BoostWave;
                float _BoostOuterOffset;

                float _CorePulseSpeed;
                float _CorePulseStrength;

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
                float d = abs(radius - targetRadius);
                float aa = max(fwidth(radius), 0.001);
                return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, d);
            }

            float AngleDistance(float a, float b)
            {
                return abs(atan2(sin(a - b), cos(a - b)));
            }

            float SignedAngleDistance(float a, float b)
            {
                return atan2(sin(a - b), cos(a - b));
            }

            float ArcMask(float angle, float centerAngle, float halfAngle)
            {
                float d = AngleDistance(angle, centerAngle);
                float aa = 0.022;
                return 1.0 - smoothstep(halfAngle, halfAngle + aa, d);
            }

            float ScanBand(float value, float center, float width)
            {
                float d = abs(value - center);
                float aa = max(fwidth(value), 0.01);
                return 1.0 - smoothstep(width, width + aa, d);
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
                float relativeAngle = SignedAngleDistance(angle, directionAngle);

                // =========================================================
                // Animation
                // =========================================================

                float corePulseSpeed = _CorePulseSpeed * lerp(1.0, 1.6, _Momentum);
                float corePulse = sin(_Time.y * corePulseSpeed) * 0.5 + 0.5;

                float slotPulseA = sin(_Time.y * _SlotPulseSpeed) * 0.5 + 0.5;
                float slotPulseB = sin(_Time.y * _SlotPulseSpeed + 2.4) * 0.5 + 0.5;

                // =========================================================
                // Inner Core
                // =========================================================

                float pulseStrength = _CorePulseStrength * lerp(1.0, 0.5, _Momentum);

                float currentCoreRadius = lerp(_CoreRadius, _CoreRadius * 1.08, _Momentum);
                currentCoreRadius *= 1.0 + corePulse * pulseStrength;
                currentCoreRadius *= 1.0 + _BoostFlash * 0.12;

                float innerCore = CircleAA(radius, currentCoreRadius);
                float coreOutline = RingAA(radius, _CoreOutlineRadius, _CoreOutlineWidth);

                // =========================================================
                // Inner Rotor
                // =========================================================

                float rotorRotationRadians = _RotorRotationPhase * PI / 180.0;
                float rotorAngle = SignedAngleDistance(relativeAngle, rotorRotationRadians);
                float rotorHalfAngle = _RotorSegmentAngle * 0.5 * PI / 180.0;

                // 三个等距机械 Rotor 段
                float rotorA = ArcMask(rotorAngle, 0.0, rotorHalfAngle);
                float rotorB = ArcMask(rotorAngle, PI * 2.0 / 3.0, rotorHalfAngle);
                float rotorC = ArcMask(rotorAngle, -PI * 2.0 / 3.0, rotorHalfAngle);

                float rotorAngularMask = max(rotorA, max(rotorB, rotorC));

                float rotorRing = RingAA(radius, _RotorRadius, _RotorWidth);
                float rotor = rotorRing * rotorAngularMask;

                float rotorEnergy = sin(rotorAngle * 3.0 - _Time.y * 3.5) * 0.5 + 0.5;
                float rotorBrightness = lerp(0.8, 1.3, rotorEnergy);
                rotorBrightness *= lerp(1.0, 1.35, _Momentum);
                rotorBrightness *= 1.0 + _BoostFlash * 0.45;

                // =========================================================
                // Middle Ring Scan
                // =========================================================

                float middleRingShape = RingAA(radius, _MiddleRadius, _MiddleWidth);

                float middleScanSpeed = _ScanSpeed * lerp(1.0, 3.0, _Momentum);
                float middleScanCenter = _Time.y * middleScanSpeed;
                float middleScanHalfAngle = _MiddleScanAngle * 0.5 * PI / 180.0;

                float middleScanA = ArcMask(angle, middleScanCenter, middleScanHalfAngle);
                float middleScanB = ArcMask(angle, middleScanCenter + PI, middleScanHalfAngle * 0.6);

                float middleBase = middleRingShape * 0.3;
                float middleScan = middleRingShape * saturate(middleScanA + middleScanB * 0.65);

                // =========================================================
                // Fixed Outer Segments
                // =========================================================

                float outerHalfAngle = _OuterSegmentAngle * 0.5 * PI / 180.0;

                float outerA = ArcMask(relativeAngle, PI * 0.25, outerHalfAngle);
                float outerB = ArcMask(relativeAngle, -PI * 0.25, outerHalfAngle);
                float outerC = ArcMask(relativeAngle, PI * 0.75, outerHalfAngle);
                float outerD = ArcMask(relativeAngle, -PI * 0.75, outerHalfAngle);

                float outerAngularMask = max(max(outerA, outerB), max(outerC, outerD));

                // 几何不旋转，只在 Boost 时径向内收/回弹
                float currentOuterRadius = _OuterRadius + _BoostWave * _BoostOuterOffset;
                float currentOuterWidth = lerp(_OuterWidth, _OuterWidth * 1.16, _Momentum);

                float outerSegments = RingAA(radius, currentOuterRadius, currentOuterWidth) * outerAngularMask;

                // 亮度在固定外壳内部流动
                float outerFlow = sin(relativeAngle * 5.0 - _Time.y * _OuterFlowSpeed) * 0.5 + 0.5;
                float outerBrightness = lerp(1.0 - _OuterFlowStrength, 1.0 + _OuterFlowStrength, outerFlow);

                outerBrightness *= lerp(1.0, 1.3, _Momentum);
                outerBrightness *= 1.0 + _BoostFlash * 0.65;

                // =========================================================
                // Fixed Element Slots
                // =========================================================

                float slotHalfAngle = _SlotAngle * 0.5 * PI / 180.0;
                float slotEndBorderRadians = _SlotEndBorderAngle * PI / 180.0;
                float slotInnerHalfAngle = max(slotHalfAngle - slotEndBorderRadians, 0.01);

                float slotACenter = PI * 0.5;
                float slotBCenter = -PI * 0.5;

                float slotAOuterAngle = ArcMask(relativeAngle, slotACenter, slotHalfAngle);
                float slotBOuterAngle = ArcMask(relativeAngle, slotBCenter, slotHalfAngle);

                float slotOuterRing = RingAA(radius, _SlotRadius, _SlotBorderWidth);

                float slotAOuter = slotOuterRing * slotAOuterAngle;
                float slotBOuter = slotOuterRing * slotBOuterAngle;

                float slotAInnerAngle = ArcMask(relativeAngle, slotACenter, slotInnerHalfAngle);
                float slotBInnerAngle = ArcMask(relativeAngle, slotBCenter, slotInnerHalfAngle);

                float slotInnerRing = RingAA(radius, _SlotRadius, _SlotWidth);

                float slotAInner = slotInnerRing * slotAInnerAngle;
                float slotBInner = slotInnerRing * slotBInnerAngle;

                float slotABorder = saturate(slotAOuter - slotAInner);
                float slotBBorder = saturate(slotBOuter - slotBInner);

                float slotAFill = slotAInner;
                float slotBFill = slotBInner;

                // =========================================================
                // Element Slot Flow
                // =========================================================

                float slotALocal = SignedAngleDistance(relativeAngle, slotACenter) / max(slotInnerHalfAngle, 0.001);
                float slotBLocal = SignedAngleDistance(relativeAngle, slotBCenter) / max(slotInnerHalfAngle, 0.001);

                float slotScanPhaseA = frac(_Time.y * _SlotScanSpeed);
                float slotScanPhaseB = frac(_Time.y * _SlotScanSpeed + 0.42);

                float slotScanPositionA = lerp(-1.15, 1.15, slotScanPhaseA);
                float slotScanPositionB = lerp(1.15, -1.15, slotScanPhaseB);

                float slotScanA = ScanBand(slotALocal, slotScanPositionA, _SlotScanWidth);
                float slotScanB = ScanBand(slotBLocal, slotScanPositionB, _SlotScanWidth);

                slotScanA *= slotAFill * _SlotAActive;
                slotScanB *= slotBFill * _SlotBActive;

                // =========================================================
                // Direction Marker
                // =========================================================

                float markerDistance = _MarkerDistance + _BoostFlash * 0.018;
                float forwardMarker = TriangleMarkerAA(p, direction, markerDistance, _ForwardMarkerSize);

                // =========================================================
                // Halo
                // =========================================================

                float halo = 1.0 - smoothstep(0.18, 0.68, radius);
                float haloPulse = lerp(0.86, 1.14, corePulse);

                halo *= (_HaloStrength + _Momentum * 0.08 + _BoostFlash * 0.06) * haloPulse;

                // =========================================================
                // Colors
                // =========================================================

                float coreBrightness = lerp(1.05, 1.80, _Momentum);
                coreBrightness *= lerp(0.90, 1.10, corePulse);
                coreBrightness *= 1.0 + _BoostFlash * 0.85;

                float3 boostCoreColor = lerp(_InnerColor.rgb, float3(1,1,1), _BoostFlash);

                float markerBrightness = lerp(1.0, 1.28, _Momentum);
                markerBrightness *= 1.0 + _BoostFlash * 0.9;

                float3 inactiveSlotColor = _EmptySlotColor.rgb * 0.55;

                float3 finalSlotAColor = lerp(inactiveSlotColor, _SlotAColor.rgb, _SlotAActive);
                float3 finalSlotBColor = lerp(inactiveSlotColor, _SlotBColor.rgb, _SlotBActive);

                float slotABaseBrightness = lerp(1.0, 1.22 + slotPulseA * _SlotPulseStrength, _SlotAActive);
                float slotBBaseBrightness = lerp(1.0, 1.22 + slotPulseB * _SlotPulseStrength, _SlotBActive);

                // =========================================================
                // Composition
                // =========================================================

                float3 color = 0;
                float alpha = 0;

                // Core
                color += boostCoreColor * innerCore * coreBrightness;
                alpha = max(alpha, innerCore);

                color += _CoreColor.rgb * coreOutline * (0.85 + _BoostFlash * 0.4);
                alpha = max(alpha, coreOutline * 0.9);

                // Rotor
                color += _RotorColor.rgb * rotor * rotorBrightness;
                alpha = max(alpha, rotor);

                // Middle Ring
                color += _StructureColor.rgb * middleBase;
                color += _CoreColor.rgb * middleScan * lerp(0.9, 1.5, _Momentum);

                alpha = max(alpha, middleBase * 0.8);
                alpha = max(alpha, middleScan);

                // Outer
                color += _OuterSegmentColor.rgb * outerSegments * outerBrightness;
                alpha = max(alpha, outerSegments);

                // Slot border
                color += _SlotBorderColor.rgb * slotABorder * 1.05;
                color += _SlotBorderColor.rgb * slotBBorder * 1.05;

                alpha = max(alpha, slotABorder * 0.95);
                alpha = max(alpha, slotBBorder * 0.95);

                // Slot base
                color += finalSlotAColor * slotAFill * slotABaseBrightness;
                color += finalSlotBColor * slotBFill * slotBBaseBrightness;

                alpha = max(alpha, slotAFill * lerp(0.72, 1.0, _SlotAActive));
                alpha = max(alpha, slotBFill * lerp(0.72, 1.0, _SlotBActive));

                // Slot flowing energy
                color += _SlotAColor.rgb * slotScanA * _SlotScanStrength;
                color += _SlotBColor.rgb * slotScanB * _SlotScanStrength;

                alpha = max(alpha, slotScanA);
                alpha = max(alpha, slotScanB);

                // Marker
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