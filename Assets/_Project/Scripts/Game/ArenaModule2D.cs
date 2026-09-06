using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class ArenaModule2D : MonoBehaviour
{
    public enum ModuleShape
    {
        Straight,
        Arc,
        Wedge
    }

    [Header("模块类型")]
    [SerializeField] private ModuleShape shape = ModuleShape.Straight;

    [Header("通用尺寸")]
    [SerializeField] private float width = 3f;
    [SerializeField] private float height = 0.65f;

    [Header("Straight")]
    [SerializeField] private float cornerCut = 0.15f;

    [Header("Arc")]
    [SerializeField] private float arcRadius = 1.7f;
    [SerializeField] private float arcThickness = 0.5f;
    [Range(20f, 180f)] [SerializeField] private float arcAngle = 90f;
    [Range(6, 48)] [SerializeField] private int arcSegments = 24;

    [Header("线条")]
    [SerializeField] private LineRenderer outlineLine;
    [SerializeField] private LineRenderer flowLine;
    [SerializeField] private float outlineWidth = 0.035f;
    [SerializeField] private float flowWidth = 0.055f;

    [Header("流光路径")]
    [SerializeField] private bool oppositeFlowEdge;
    [SerializeField] private float flowInset = 0.10f;

    [Header("流光动画")]
    [Range(0f, 1f)] [SerializeField] private float flowOffset;
    [SerializeField] private float flowSpeed = 0.22f;
    [SerializeField] private float flowStrength = 1.35f;
    [SerializeField] private bool reverseFlow;

    private static readonly int FlowOffsetID = Shader.PropertyToID("_FlowOffset");
    private static readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");
    private static readonly int FlowStrengthID = Shader.PropertyToID("_FlowStrength");

    private MeshFilter meshFilter;
    private PolygonCollider2D polygonCollider;
    private Mesh generatedMesh;
    private MaterialPropertyBlock flowPropertyBlock;

    private void OnEnable()
    {
        CacheComponents();
        EnsureMesh();
        EnsurePropertyBlock();
        Rebuild();
    }

    private void OnValidate()
    {
        width = Mathf.Max(0.2f, width);
        height = Mathf.Max(0.1f, height);
        arcRadius = Mathf.Max(0.2f, arcRadius);
        arcThickness = Mathf.Clamp(arcThickness, 0.05f, arcRadius * 1.8f);
        arcSegments = Mathf.Clamp(arcSegments, 6, 48);
        flowInset = Mathf.Max(0f, flowInset);

        CacheComponents();
        EnsureMesh();
        EnsurePropertyBlock();
        Rebuild();
    }

    private void Update()
    {
        if (!Application.isPlaying) ApplyFlowProperties();
    }

    private void CacheComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (polygonCollider == null) polygonCollider = GetComponent<PolygonCollider2D>();
    }

    private void EnsureMesh()
    {
        if (generatedMesh != null) return;

        generatedMesh = new Mesh { name = "ArenaModule_GeneratedMesh" };
        meshFilter.sharedMesh = generatedMesh;
    }

    private void EnsurePropertyBlock()
    {
        if (flowPropertyBlock == null) flowPropertyBlock = new MaterialPropertyBlock();
    }

    private void Rebuild()
    {
        if (generatedMesh == null || polygonCollider == null) return;

        switch (shape)
        {
            case ModuleShape.Straight:
                BuildStraight();
                break;

            case ModuleShape.Arc:
                BuildArc();
                break;

            case ModuleShape.Wedge:
                BuildWedge();
                break;
        }

        SetupLineRenderer(outlineLine, outlineWidth, true);
        SetupLineRenderer(flowLine, flowWidth, false);
        ApplyFlowProperties();
    }

    private void BuildStraight()
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float cut = Mathf.Min(cornerCut, halfWidth * 0.45f, halfHeight * 0.45f);

        Vector2[] points =
        {
            new Vector2(-halfWidth + cut, -halfHeight),
            new Vector2( halfWidth - cut, -halfHeight),
            new Vector2( halfWidth, -halfHeight + cut),
            new Vector2( halfWidth,  halfHeight - cut),
            new Vector2( halfWidth - cut,  halfHeight),
            new Vector2(-halfWidth + cut,  halfHeight),
            new Vector2(-halfWidth,  halfHeight - cut),
            new Vector2(-halfWidth, -halfHeight + cut)
        };

        BuildPolygonMesh(points);
        ApplyCollider(points);
        ApplyOutline(points);

        float edgeY = oppositeFlowEdge ? -halfHeight + flowInset : halfHeight - flowInset;

        Vector2[] flowPoints =
        {
            new Vector2(-halfWidth + cut * 1.8f, edgeY),
            new Vector2( halfWidth - cut * 1.8f, edgeY)
        };

        ApplyFlow(flowPoints);
    }

    private void BuildWedge()
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        Vector2[] points =
        {
            new Vector2(-halfWidth, -halfHeight * 0.32f),
            new Vector2( halfWidth, -halfHeight),
            new Vector2( halfWidth,  halfHeight),
            new Vector2(-halfWidth,  halfHeight * 0.32f)
        };

        BuildPolygonMesh(points);
        ApplyCollider(points);
        ApplyOutline(points);

        Vector2 start;
        Vector2 end;

        if (!oppositeFlowEdge)
        {
            start = new Vector2(-halfWidth + flowInset, halfHeight * 0.32f - flowInset * 0.25f);
            end = new Vector2(halfWidth - flowInset, halfHeight - flowInset);
        }
        else
        {
            start = new Vector2(-halfWidth + flowInset, -halfHeight * 0.32f + flowInset * 0.25f);
            end = new Vector2(halfWidth - flowInset, -halfHeight + flowInset);
        }

        ApplyFlow(new[] { start, end });
    }

    private void BuildArc()
    {
        float outerRadius = arcRadius + arcThickness * 0.5f;
        float innerRadius = Mathf.Max(0.05f, arcRadius - arcThickness * 0.5f);

        float startAngle = -arcAngle * 0.5f;
        float endAngle = arcAngle * 0.5f;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> colliderPoints = new List<Vector2>();
        List<Vector2> flowPoints = new List<Vector2>();

        float flowRadius = oppositeFlowEdge ? innerRadius + flowInset : outerRadius - flowInset;
        flowRadius = Mathf.Clamp(flowRadius, innerRadius + 0.02f, outerRadius - 0.02f);

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float radians = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            vertices.Add(direction * outerRadius);
            vertices.Add(direction * innerRadius);

            flowPoints.Add(direction * flowRadius);
        }

        for (int i = 0; i < arcSegments; i++)
        {
            int index = i * 2;

            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);

            triangles.Add(index + 2);
            triangles.Add(index + 3);
            triangles.Add(index + 1);
        }

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float radians = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            colliderPoints.Add(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * outerRadius);
        }

        for (int i = arcSegments; i >= 0; i--)
        {
            float t = i / (float)arcSegments;
            float radians = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            colliderPoints.Add(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * innerRadius);
        }

        generatedMesh.Clear();
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateBounds();

        ApplyCollider(colliderPoints.ToArray());
        ApplyOutline(colliderPoints.ToArray());
        ApplyFlow(flowPoints.ToArray());
    }

    private void BuildPolygonMesh(Vector2[] points)
    {
        Vector3[] vertices = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++) vertices[i] = new Vector3(points[i].x, points[i].y, 0f);

        int[] triangles = new int[(points.Length - 2) * 3];
        int triangleIndex = 0;

        for (int i = 1; i < points.Length - 1; i++)
        {
            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = i;
            triangles[triangleIndex++] = i + 1;
        }

        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateBounds();
    }

    private void ApplyCollider(Vector2[] points)
    {
        polygonCollider.pathCount = 1;
        polygonCollider.SetPath(0, points);
    }

    private void ApplyOutline(Vector2[] points)
    {
        if (outlineLine == null) return;

        Vector3[] positions = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++) positions[i] = new Vector3(points[i].x, points[i].y, -0.01f);

        outlineLine.positionCount = positions.Length;
        outlineLine.SetPositions(positions);
        outlineLine.loop = true;
    }

    private void ApplyFlow(Vector2[] points)
    {
        if (flowLine == null) return;

        Vector3[] positions = new Vector3[points.Length];

        if (!reverseFlow)
        {
            for (int i = 0; i < points.Length; i++) positions[i] = new Vector3(points[i].x, points[i].y, -0.02f);
        }
        else
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 point = points[points.Length - 1 - i];
                positions[i] = new Vector3(point.x, point.y, -0.02f);
            }
        }

        flowLine.positionCount = positions.Length;
        flowLine.SetPositions(positions);
        flowLine.loop = false;
    }

    private void ApplyFlowProperties()
    {
        if (flowLine == null) return;

        EnsurePropertyBlock();

        flowLine.GetPropertyBlock(flowPropertyBlock);

        flowPropertyBlock.SetFloat(FlowOffsetID, flowOffset);
        flowPropertyBlock.SetFloat(FlowSpeedID, Mathf.Abs(flowSpeed));
        flowPropertyBlock.SetFloat(FlowStrengthID, flowStrength);

        flowLine.SetPropertyBlock(flowPropertyBlock);
    }

    private void SetupLineRenderer(LineRenderer line, float lineWidth, bool loop)
    {
        if (line == null) return;

        line.useWorldSpace = false;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Stretch;
        line.widthMultiplier = lineWidth;
        line.loop = loop;

        line.numCornerVertices = 0;
        line.numCapVertices = loop ? 0 : 2;
    }
}