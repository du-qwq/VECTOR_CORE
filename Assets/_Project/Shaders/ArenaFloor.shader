Shader "VECTOR_CORE/ArenaFloor"
{
    Properties
    {
        [Header(Base)]
        _BaseColor("Base Color", Color) = (0.018,0.025,0.035,1)
        _EdgeDarkening("Edge Darkening", Range(0,0.5)) = 0.16

        [Header(Grid)]
        _MinorColor("Minor Grid Color", Color) = (0.15,0.19,0.24,0.08)
        _MajorColor("Major Grid Color", Color) = (0.22,0.28,0.35,0.16)
        _GridSpacing("Grid Spacing", Float) = 1
        _MajorSpacing("Major Grid Spacing", Float) = 4
        _MinorWidth("Minor Grid Width", Range(0,2)) = 0.15
        _MajorWidth("Major Grid Width", Range(0,2)) = 0.3

        [Header(Arena Frame)]
        _ArenaSize("Arena Size XY", Vector) = (32,18,0,0)
        _FrameInset("Frame Inset", Float) = 0.4
        _CornerCut("Corner Cut", Float) = 0.7
        _FrameWidth("Frame Width", Range(0.01,0.3)) = 0.05
        _FrameColor("Frame Color", Color) = (0.32,0.39,0.47,0.55)

        [Header(Accent Markers)]
        _MarkerLength("Marker Length", Float) = 1.2
        _MarkerWidth("Marker Width", Float) = 0.08
        [HDR]_AccentColor("Accent Color", Color) = (0.19,0.84,1,0.9)

        [Header(Center Zone)]
        _CenterRadius("Center Radius", Float) = 2.1
        _CenterRingWidth("Center Ring Width", Range(0.01,0.2)) = 0.045
        _CenterTickLength("Center Tick Length", Float) = 0.45
        _CenterColor("Center Color", Color) = (0.25,0.34,0.42,0.5)

        [Header(Structure Marks)]
        _StructureWidth("Structure Line Width", Range(0.01,0.2)) = 0.04
        _StructureColor("Structure Color", Color) = (0.20,0.27,0.34,0.42)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

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
                float4 _BaseColor;
                float4 _MinorColor;
                float4 _MajorColor;
                float4 _ArenaSize;
                float4 _FrameColor;
                float4 _AccentColor;
                float4 _CenterColor;
                float4 _StructureColor;

                float _EdgeDarkening;

                float _GridSpacing;
                float _MajorSpacing;
                float _MinorWidth;
                float _MajorWidth;

                float _FrameInset;
                float _CornerCut;
                float _FrameWidth;

                float _MarkerLength;
                float _MarkerWidth;

                float _CenterRadius;
                float _CenterRingWidth;
                float _CenterTickLength;

                float _StructureWidth;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float GridAA(float2 position, float spacing, float width)
            {
                float2 coord = position / spacing;
                float2 grid = abs(frac(coord - 0.5) - 0.5) / max(fwidth(coord), 0.0001);
                float lineDistance = min(grid.x, grid.y);
                return 1.0 - smoothstep(width, width + 1.0, lineDistance);
            }

            float SdChamferedRect(float2 position, float2 halfSize, float cornerCut)
            {
                float2 p = abs(position);
                float boxDistance = max(p.x - halfSize.x, p.y - halfSize.y);
                float cornerDistance = (p.x + p.y - (halfSize.x + halfSize.y - cornerCut)) * 0.70710678;
                return max(boxDistance, cornerDistance);
            }

            float SdBox(float2 position, float2 halfSize)
            {
                float2 q = abs(position) - halfSize;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            float FillAA(float distance)
            {
                float aa = max(fwidth(distance), 0.0005);
                return 1.0 - smoothstep(0.0, aa, distance);
            }

            float OutlineAA(float distance, float width)
            {
                float aa = max(fwidth(distance), 0.0005);
                return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, abs(distance));
            }

            float RingAA(float2 position, float radius, float width)
            {
                float distanceToRing = abs(length(position) - radius);
                float aa = max(fwidth(distanceToRing), 0.0005);
                return 1.0 - smoothstep(width * 0.5, width * 0.5 + aa, distanceToRing);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 arenaPosition = (input.uv - 0.5) * _ArenaSize.xy;
                float2 halfSize = _ArenaSize.xy * 0.5 - _FrameInset;

                float frameDistance = SdChamferedRect(arenaPosition, halfSize, _CornerCut);
                float frameAA = max(fwidth(frameDistance), 0.0005);

                float insideArena = 1.0 - smoothstep(0.0, frameAA, frameDistance);
                float frame = OutlineAA(frameDistance, _FrameWidth);

                float minorGrid = GridAA(arenaPosition, _GridSpacing, _MinorWidth) * insideArena;
                float majorGrid = GridAA(arenaPosition, _MajorSpacing, _MajorWidth) * insideArena;

                float3 color = _BaseColor.rgb;
                color = lerp(color, _MinorColor.rgb, minorGrid * _MinorColor.a);
                color = lerp(color, _MajorColor.rgb, majorGrid * _MajorColor.a);

                color *= lerp(0.62, 1.0, insideArena);
                color = lerp(color, _FrameColor.rgb, frame * _FrameColor.a);

                float topMarker = SdBox(arenaPosition - float2(0.0, halfSize.y), float2(_MarkerLength * 0.5, _MarkerWidth * 0.5));
                float bottomMarker = SdBox(arenaPosition - float2(0.0, -halfSize.y), float2(_MarkerLength * 0.5, _MarkerWidth * 0.5));
                float leftMarker = SdBox(arenaPosition - float2(-halfSize.x, 0.0), float2(_MarkerWidth * 0.5, _MarkerLength * 0.5));
                float rightMarker = SdBox(arenaPosition - float2(halfSize.x, 0.0), float2(_MarkerWidth * 0.5, _MarkerLength * 0.5));

                float marker = max(max(FillAA(topMarker), FillAA(bottomMarker)), max(FillAA(leftMarker), FillAA(rightMarker)));
                color = lerp(color, _AccentColor.rgb, marker * _AccentColor.a);

                float centerRing = RingAA(arenaPosition, _CenterRadius, _CenterRingWidth);

                float centerTickTop = SdBox(arenaPosition - float2(0.0, _CenterRadius), float2(0.035, _CenterTickLength * 0.5));
                float centerTickBottom = SdBox(arenaPosition - float2(0.0, -_CenterRadius), float2(0.035, _CenterTickLength * 0.5));
                float centerTickLeft = SdBox(arenaPosition - float2(-_CenterRadius, 0.0), float2(_CenterTickLength * 0.5, 0.035));
                float centerTickRight = SdBox(arenaPosition - float2(_CenterRadius, 0.0), float2(_CenterTickLength * 0.5, 0.035));

                float centerTicks = max(max(FillAA(centerTickTop), FillAA(centerTickBottom)), max(FillAA(centerTickLeft), FillAA(centerTickRight)));

                float centerHorizontal = FillAA(SdBox(arenaPosition, float2(0.35, 0.025)));
                float centerVertical = FillAA(SdBox(arenaPosition, float2(0.025, 0.35)));

                float centerMask = max(centerRing, max(centerTicks, max(centerHorizontal, centerVertical)));
                color = lerp(color, _CenterColor.rgb, centerMask * _CenterColor.a);

                float structureA = OutlineAA(SdBox(arenaPosition - float2(-8.2, 4.5), float2(2.1, 0.75)), _StructureWidth);
                float structureB = OutlineAA(SdBox(arenaPosition - float2(8.7, 3.6), float2(0.75, 1.8)), _StructureWidth);
                float structureC = OutlineAA(SdBox(arenaPosition - float2(-8.7, -3.8), float2(0.75, 1.65)), _StructureWidth);
                float structureD = OutlineAA(SdBox(arenaPosition - float2(8.0, -4.6), float2(2.25, 0.7)), _StructureWidth);

                float detailA = FillAA(SdBox(arenaPosition - float2(-6.0, 4.5), float2(0.35, 0.035)));
                float detailB = FillAA(SdBox(arenaPosition - float2(8.7, 1.6), float2(0.035, 0.35)));
                float detailC = FillAA(SdBox(arenaPosition - float2(-8.7, -5.65), float2(0.035, 0.35)));
                float detailD = FillAA(SdBox(arenaPosition - float2(10.4, -4.6), float2(0.35, 0.035)));

                float structureMask = max(max(structureA, structureB), max(structureC, structureD));
                structureMask = max(structureMask, max(max(detailA, detailB), max(detailC, detailD)));

                color = lerp(color, _StructureColor.rgb, structureMask * _StructureColor.a);

                float2 edgeUV = abs(input.uv - 0.5) * 2.0;
                float edge = smoothstep(0.72, 1.0, max(edgeUV.x, edgeUV.y));
                color *= 1.0 - edge * _EdgeDarkening;

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}