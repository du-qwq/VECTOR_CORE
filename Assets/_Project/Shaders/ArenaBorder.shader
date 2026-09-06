Shader "VECTOR_CORE/ArenaBorder"
{
    Properties
    {
        [Header(Arena)]
        _ArenaSize("Arena Size", Vector) = (32,18,0,0)
        _Chamfer("Corner Chamfer", Range(0,3)) = 0.75

        [Header(Border)]
        _BorderDepth("Border Depth", Range(0.05,1)) = 0.30
        _InnerLineInset("Inner Line Inset", Range(0,1)) = 0.18
        _InnerLineWidth("Inner Line Width", Range(0.005,0.15)) = 0.035

        _BorderColor("Border Color", Color) = (0.045,0.075,0.10,0.85)
        _EdgeColor("Edge Color", Color) = (0.17,0.26,0.32,0.75)

        [Header(Flow)]
        [HDR]_FlowColor("Flow Color", Color) = (0.40,0.91,1,1)
        _FlowSpeed("Flow Speed", Range(0,2)) = 0.22
        _FlowLength("Flow Length", Range(0.01,0.5)) = 0.12
        _FlowSoftness("Flow Softness", Range(0.005,0.2)) = 0.055
        _FlowIntensity("Flow Intensity", Range(0,4)) = 1.4
        _FlowLineWidth("Flow Line Width", Range(0.005,0.15)) = 0.050

        [Header(Ticks)]
        _TickSpacing("Tick Spacing", Range(0.5,5)) = 2
        _TickLength("Tick Length", Range(0.03,0.5)) = 0.13
        _TickWidth("Tick Width", Range(0.005,0.15)) = 0.025
        _TickAlpha("Tick Alpha", Range(0,1)) = 0.30

        [Header(Corners)]
        _CornerAccentLength("Corner Accent Length", Range(0.1,2)) = 0.65
        _CornerAccentWidth("Corner Accent Width", Range(0.005,0.15)) = 0.035
        _CornerAccentAlpha("Corner Accent Alpha", Range(0,1)) = 0.55
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
                float4 _ArenaSize;

                float _Chamfer;
                float _BorderDepth;
                float _InnerLineInset;
                float _InnerLineWidth;

                float4 _BorderColor;
                float4 _EdgeColor;

                float4 _FlowColor;
                float _FlowSpeed;
                float _FlowLength;
                float _FlowSoftness;
                float _FlowIntensity;
                float _FlowLineWidth;

                float _TickSpacing;
                float _TickLength;
                float _TickWidth;
                float _TickAlpha;

                float _CornerAccentLength;
                float _CornerAccentWidth;
                float _CornerAccentAlpha;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float ChamferBoxSDF(float2 p, float2 halfSize, float chamfer)
            {
                float2 ap = abs(p);

                float boxDistance = max(ap.x - halfSize.x, ap.y - halfSize.y);

                float diagonalDistance =
                    (ap.x + ap.y - (halfSize.x + halfSize.y - chamfer)) * 0.70710678;

                return max(boxDistance, diagonalDistance);
            }

            float InsideChamferBox(float2 p, float2 halfSize, float chamfer)
            {
                float sdf = ChamferBoxSDF(p, halfSize, chamfer);
                float aa = max(fwidth(sdf), 0.001);

                return 1.0 - smoothstep(-aa, aa, sdf);
            }

            float ChamferLine(float2 p, float2 halfSize, float chamfer, float width)
            {
                float sdf = abs(ChamferBoxSDF(p, halfSize, chamfer));
                float aa = max(fwidth(sdf), 0.001);

                return 1.0 - smoothstep(width, width + aa, sdf);
            }

            float Band(float value, float center, float halfWidth, float softness)
            {
                float d = abs(value - center);
                return 1.0 - smoothstep(halfWidth, halfWidth + softness, d);
            }

            float FlowPulse(float u, float offset)
            {
                float phase = frac(u - _Time.y * _FlowSpeed + offset);
                float d = abs(phase - 0.5);

                return 1.0 - smoothstep(
                    _FlowLength,
                    _FlowLength + _FlowSoftness,
                    d
                );
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 将 UV 转成真实 Arena 单位。
                float2 p = (input.uv - 0.5) * _ArenaSize.xy;

                float2 halfSize = _ArenaSize.xy * 0.5;

                // =========================================================
                // Main border body
                // =========================================================

                float outerMask = InsideChamferBox(p, halfSize, _Chamfer);

                float2 innerHalfSize = halfSize - _BorderDepth;
                float innerChamfer = max(0.05, _Chamfer - _BorderDepth * 0.35);

                float innerMask = InsideChamferBox(p, innerHalfSize, innerChamfer);

                float borderBody = saturate(outerMask - innerMask);

                // =========================================================
                // Inner structural line
                // =========================================================

                float2 innerLineHalfSize =
                    halfSize - (_BorderDepth + _InnerLineInset);

                float innerLineChamfer =
                    max(0.05, _Chamfer - (_BorderDepth + _InnerLineInset) * 0.35);

                float innerLine = ChamferLine(
                    p,
                    innerLineHalfSize,
                    innerLineChamfer,
                    _InnerLineWidth
                );

                // =========================================================
                // Side masks
                // =========================================================

                float ax = abs(p.x);
                float ay = abs(p.y);

                float horizontalEdgeDistance =
                    abs(ay - innerLineHalfSize.y);

                float verticalEdgeDistance =
                    abs(ax - innerLineHalfSize.x);

                float horizontalLineMask =
                    1.0 - smoothstep(
                        _FlowLineWidth,
                        _FlowLineWidth + 0.02,
                        horizontalEdgeDistance
                    );

                float verticalLineMask =
                    1.0 - smoothstep(
                        _FlowLineWidth,
                        _FlowLineWidth + 0.02,
                        verticalEdgeDistance
                    );

                // 不让边缘流光跑进切角区域。
                float horizontalValid =
                    1.0 - smoothstep(
                        innerLineHalfSize.x - _Chamfer - 0.2,
                        innerLineHalfSize.x - _Chamfer + 0.1,
                        ax
                    );

                float verticalValid =
                    1.0 - smoothstep(
                        innerLineHalfSize.y - _Chamfer - 0.2,
                        innerLineHalfSize.y - _Chamfer + 0.1,
                        ay
                    );

                // =========================================================
                // Four independent flow signals
                // =========================================================

                float horizontalU =
                    saturate((p.x + innerLineHalfSize.x) /
                    max(innerLineHalfSize.x * 2.0, 0.001));

                float verticalU =
                    saturate((p.y + innerLineHalfSize.y) /
                    max(innerLineHalfSize.y * 2.0, 0.001));

                float topMask = horizontalLineMask *
                    step(0.0, p.y) *
                    horizontalValid;

                float bottomMask = horizontalLineMask *
                    step(p.y, 0.0) *
                    horizontalValid;

                float rightMask = verticalLineMask *
                    step(0.0, p.x) *
                    verticalValid;

                float leftMask = verticalLineMask *
                    step(p.x, 0.0) *
                    verticalValid;

                float topFlow =
                    topMask * FlowPulse(horizontalU, 0.03);

                float bottomFlow =
                    bottomMask * FlowPulse(1.0 - horizontalU, 0.47);

                float rightFlow =
                    rightMask * FlowPulse(1.0 - verticalU, 0.71);

                float leftFlow =
                    leftMask * FlowPulse(verticalU, 0.24);

                float flow =
                    saturate(topFlow + bottomFlow + rightFlow + leftFlow);

                // =========================================================
                // Edge ticks
                // =========================================================

                float safeTickSpacing = max(_TickSpacing, 0.05);

                float tickX =
                    abs(frac((p.x + halfSize.x) / safeTickSpacing) - 0.5);

                float tickY =
                    abs(frac((p.y + halfSize.y) / safeTickSpacing) - 0.5);

                float tickAlongX =
                    1.0 - smoothstep(
                        _TickWidth,
                        _TickWidth + 0.025,
                        tickX * safeTickSpacing
                    );

                float tickAlongY =
                    1.0 - smoothstep(
                        _TickWidth,
                        _TickWidth + 0.025,
                        tickY * safeTickSpacing
                    );

                float topTick =
                    tickAlongX *
                    Band(
                        p.y,
                        innerLineHalfSize.y - _TickLength,
                        _TickLength,
                        0.015
                    ) *
                    horizontalValid;

                float bottomTick =
                    tickAlongX *
                    Band(
                        p.y,
                        -innerLineHalfSize.y + _TickLength,
                        _TickLength,
                        0.015
                    ) *
                    horizontalValid;

                float leftTick =
                    tickAlongY *
                    Band(
                        p.x,
                        -innerLineHalfSize.x + _TickLength,
                        _TickLength,
                        0.015
                    ) *
                    verticalValid;

                float rightTick =
                    tickAlongY *
                    Band(
                        p.x,
                        innerLineHalfSize.x - _TickLength,
                        _TickLength,
                        0.015
                    ) *
                    verticalValid;

                float ticks =
                    saturate(topTick + bottomTick + leftTick + rightTick);

                // =========================================================
                // Corner accents
                // =========================================================

                float cornerX =
                    1.0 - smoothstep(
                        _CornerAccentLength,
                        _CornerAccentLength + 0.05,
                        abs(ax - (halfSize.x - _Chamfer))
                    );

                float cornerY =
                    1.0 - smoothstep(
                        _CornerAccentLength,
                        _CornerAccentLength + 0.05,
                        abs(ay - (halfSize.y - _Chamfer))
                    );

                float diagonalSDF =
                    abs(
                        ax + ay -
                        (halfSize.x + halfSize.y - _Chamfer)
                    ) * 0.70710678;

                float cornerDiagonal =
                    1.0 - smoothstep(
                        _CornerAccentWidth,
                        _CornerAccentWidth + 0.025,
                        diagonalSDF
                    );

                float nearCorner =
                    step(halfSize.x - _Chamfer - _CornerAccentLength, ax) *
                    step(halfSize.y - _Chamfer - _CornerAccentLength, ay);

                float cornerAccent =
                    cornerDiagonal *
                    nearCorner *
                    saturate(cornerX + cornerY);

                // =========================================================
                // Composition
                // =========================================================

                float3 color = 0;
                float alpha = 0;

                color += _BorderColor.rgb * borderBody;
                alpha = max(alpha, borderBody * _BorderColor.a);

                color += _EdgeColor.rgb * innerLine;
                alpha = max(alpha, innerLine * _EdgeColor.a);

                color += _EdgeColor.rgb * ticks * _TickAlpha;
                alpha = max(alpha, ticks * _TickAlpha);

                color += _FlowColor.rgb *
                    flow *
                    _FlowIntensity;

                alpha = max(alpha, flow);

                color += _FlowColor.rgb *
                    cornerAccent *
                    _CornerAccentAlpha;

                alpha = max(
                    alpha,
                    cornerAccent * _CornerAccentAlpha
                );

                return half4(color, saturate(alpha));
            }

            ENDHLSL
        }
    }
}