using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
[RequireComponent(typeof(CoreElementStorage))]
public class CoreVisualController : MonoBehaviour
{
    [Header("视觉对象")]
    [SerializeField] private MeshRenderer coreRenderer;

    [Header("方向")]
    [SerializeField] private float minDirectionSpeed = 0.3f;
    [SerializeField] private float directionSmoothSpeed = 12f;

    [Header("Momentum")]
    [SerializeField] private float momentumSmoothSpeed = 6f;

    [Header("元素颜色")]
    [SerializeField] private Color thermalColor = new Color(1f, 0.396f, 0.282f, 1f);
    [SerializeField] private Color fluidColor = new Color(0.196f, 0.824f, 0.706f, 1f);
    [SerializeField] private Color voltColor = new Color(1f, 0.831f, 0.278f, 1f);
    [SerializeField] private Color cryoColor = new Color(0.604f, 0.522f, 0.961f, 1f);

    private static readonly int MomentumID = Shader.PropertyToID("_Momentum");
    private static readonly int ForwardDirID = Shader.PropertyToID("_ForwardDir");

    private static readonly int SlotAColorID = Shader.PropertyToID("_SlotAColor");
    private static readonly int SlotBColorID = Shader.PropertyToID("_SlotBColor");
    private static readonly int SlotAActiveID = Shader.PropertyToID("_SlotAActive");
    private static readonly int SlotBActiveID = Shader.PropertyToID("_SlotBActive");

    private CoreMotor motor;
    private CoreElementStorage storage;
    private MaterialPropertyBlock propertyBlock;

    private Vector2 currentDirection = Vector2.up;
    private float currentMomentum;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        storage = GetComponent<CoreElementStorage>();
        propertyBlock = new MaterialPropertyBlock();

        if (coreRenderer == null) coreRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void LateUpdate()
    {
        if (coreRenderer == null) return;

        UpdateDirection();
        UpdateMomentum();
        UpdateShader();
    }

    private void UpdateDirection()
    {
        Vector2 velocity = motor.Velocity;
        if (velocity.magnitude < minDirectionSpeed) return;

        Vector2 targetDirection = velocity.normalized;
        float t = 1f - Mathf.Exp(-directionSmoothSpeed * Time.deltaTime);
        currentDirection = Vector2.Lerp(currentDirection, targetDirection, t).normalized;
    }

    private void UpdateMomentum()
    {
        currentMomentum = Mathf.MoveTowards(currentMomentum, motor.NormalizedMomentum, momentumSmoothSpeed * Time.deltaTime);
    }

    private void UpdateShader()
    {
        ElementType slotA = storage.SlotA;
        ElementType slotB = storage.SlotB;

        coreRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetVector(ForwardDirID, new Vector4(currentDirection.x, currentDirection.y, 0f, 0f));
        propertyBlock.SetFloat(MomentumID, currentMomentum);

        propertyBlock.SetColor(SlotAColorID, GetElementColor(slotA));
        propertyBlock.SetColor(SlotBColorID, GetElementColor(slotB));

        propertyBlock.SetFloat(SlotAActiveID, slotA == ElementType.None ? 0f : 1f);
        propertyBlock.SetFloat(SlotBActiveID, slotB == ElementType.None ? 0f : 1f);

        coreRenderer.SetPropertyBlock(propertyBlock);
    }

    private Color GetElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Thermal => thermalColor,
            ElementType.Fluid => fluidColor,
            ElementType.Volt => voltColor,
            ElementType.Cryo => cryoColor,
            _ => Color.white
        };
    }
}