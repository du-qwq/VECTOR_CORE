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

    [Header("Inner Rotor")]
    [SerializeField] private float rotorIdleSpeed = 28f;
    [SerializeField] private float rotorMaxSpeed = 90f;

    [Header("Boost视觉")]
    [SerializeField] private float boostVisualDuration = 0.18f;
    [SerializeField] private float boostFlashPower = 2f;

    [Header("元素颜色")]
    [SerializeField] private Color thermalColor = new Color(1f, 0.396f, 0.282f, 1f);
    [SerializeField] private Color fluidColor = new Color(0.196f, 0.824f, 0.706f, 1f);
    [SerializeField] private Color voltColor = new Color(1f, 0.831f, 0.278f, 1f);
    [SerializeField] private Color cryoColor = new Color(0.604f, 0.522f, 0.961f, 1f);

    private static readonly int MomentumID = Shader.PropertyToID("_Momentum");
    private static readonly int ForwardDirID = Shader.PropertyToID("_ForwardDir");
    private static readonly int RotorRotationPhaseID = Shader.PropertyToID("_RotorRotationPhase");

    private static readonly int BoostFlashID = Shader.PropertyToID("_BoostFlash");
    private static readonly int BoostWaveID = Shader.PropertyToID("_BoostWave");

    private static readonly int SlotAColorID = Shader.PropertyToID("_SlotAColor");
    private static readonly int SlotBColorID = Shader.PropertyToID("_SlotBColor");
    private static readonly int SlotAActiveID = Shader.PropertyToID("_SlotAActive");
    private static readonly int SlotBActiveID = Shader.PropertyToID("_SlotBActive");

    private CoreMotor motor;
    private CoreElementStorage storage;
    private MaterialPropertyBlock propertyBlock;

    private Vector2 currentDirection = Vector2.up;
    private float currentMomentum;
    private float rotorRotationPhase;

    private bool boostPlaying;
    private float boostElapsed;
    private float boostFlash;
    private float boostWave;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        storage = GetComponent<CoreElementStorage>();
        propertyBlock = new MaterialPropertyBlock();

        if (coreRenderer == null) coreRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void OnEnable()
    {
        if (motor == null) motor = GetComponent<CoreMotor>();
        motor.Boosted += OnBoosted;
    }

    private void OnDisable()
    {
        if (motor != null) motor.Boosted -= OnBoosted;
    }

    private void LateUpdate()
    {
        if (coreRenderer == null) return;

        UpdateDirection();
        UpdateMomentum();
        UpdateRotor();
        UpdateBoost();
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

    private void UpdateRotor()
    {
        float speed = Mathf.Lerp(rotorIdleSpeed, rotorMaxSpeed, currentMomentum);
        rotorRotationPhase = Mathf.Repeat(rotorRotationPhase + speed * Time.deltaTime, 360f);
    }

    private void UpdateBoost()
    {
        if (!boostPlaying)
        {
            boostFlash = 0f;
            boostWave = 0f;
            return;
        }

        boostElapsed += Time.deltaTime;
        float phase = boostVisualDuration <= 0f ? 1f : Mathf.Clamp01(boostElapsed / boostVisualDuration);

        boostFlash = Mathf.Pow(1f - phase, boostFlashPower);
        boostWave = -Mathf.Cos(phase * Mathf.PI * 2f) * (1f - phase);

        if (phase >= 1f)
        {
            boostPlaying = false;
            boostFlash = 0f;
            boostWave = 0f;
        }
    }

    private void OnBoosted()
    {
        boostElapsed = 0f;
        boostPlaying = true;
        boostFlash = 1f;
        boostWave = -1f;
    }

    private void UpdateShader()
    {
        ElementType slotA = storage.SlotA;
        ElementType slotB = storage.SlotB;

        coreRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetVector(ForwardDirID, new Vector4(currentDirection.x, currentDirection.y, 0f, 0f));
        propertyBlock.SetFloat(MomentumID, currentMomentum);
        propertyBlock.SetFloat(RotorRotationPhaseID, rotorRotationPhase);

        propertyBlock.SetFloat(BoostFlashID, boostFlash);
        propertyBlock.SetFloat(BoostWaveID, boostWave);

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