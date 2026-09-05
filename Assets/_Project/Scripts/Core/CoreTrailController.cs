using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
public class CoreTrailController : MonoBehaviour
{
    [Header("Trail")]
    [SerializeField] private TrailRenderer ghostTrail;
    [SerializeField] private TrailRenderer mainTrail;
    [SerializeField] private TrailRenderer coreTrail;

    [Header("出现条件")]
    [Range(0f, 1f)] [SerializeField] private float minMomentum = 0.12f;

    [Header("响应速度")]
    [SerializeField] private float appearSpeed = 10f;
    [SerializeField] private float disappearSpeed = 5f;

    [Header("Ghost - 宽 / 淡 / 长")]
    [SerializeField] private float ghostMinTime = 0.10f;
    [SerializeField] private float ghostMaxTime = 0.34f;
    [SerializeField] private float ghostMinWidth = 0.20f;
    [SerializeField] private float ghostMaxWidth = 0.52f;
    [SerializeField] private float ghostMinOpacity = 0.08f;
    [SerializeField] private float ghostMaxOpacity = 0.24f;

    [Header("Main - 主体")]
    [SerializeField] private float mainMinTime = 0.08f;
    [SerializeField] private float mainMaxTime = 0.27f;
    [SerializeField] private float mainMinWidth = 0.11f;
    [SerializeField] private float mainMaxWidth = 0.30f;
    [SerializeField] private float mainMinOpacity = 0.16f;
    [SerializeField] private float mainMaxOpacity = 0.62f;

    [Header("Core - 细 / 亮 / 短")]
    [SerializeField] private float coreMinTime = 0.05f;
    [SerializeField] private float coreMaxTime = 0.20f;
    [SerializeField] private float coreMinWidth = 0.035f;
    [SerializeField] private float coreMaxWidth = 0.10f;
    [SerializeField] private float coreMinOpacity = 0.20f;
    [SerializeField] private float coreMaxOpacity = 0.90f;

    [Header("Boost")]
    [SerializeField] private float boostDuration = 0.22f;
    [SerializeField] private float boostLengthMultiplier = 1.8f;
    [SerializeField] private float boostWidthMultiplier = 1.35f;
    [SerializeField] private float boostOpacityMultiplier = 1.35f;

    [Header("Boost Shader")]
    [SerializeField] private float ghostBoostStrength = 0.8f;
    [SerializeField] private float mainBoostStrength = 1.25f;
    [SerializeField] private float coreBoostStrength = 1.8f;

    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
    private static readonly int BoostID = Shader.PropertyToID("_Boost");
    private static readonly int BoostIntensityID = Shader.PropertyToID("_BoostIntensity");

    private CoreMotor motor;

    private MaterialPropertyBlock ghostBlock;
    private MaterialPropertyBlock mainBlock;
    private MaterialPropertyBlock coreBlock;

    private float currentTrailFactor;
    private float boostTimer;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();

        ghostBlock = new MaterialPropertyBlock();
        mainBlock = new MaterialPropertyBlock();
        coreBlock = new MaterialPropertyBlock();

        SetupTrailCurves();
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
        UpdateBoost();
        UpdateTrailFactor();
        UpdateTrails();
    }

    private void SetupTrailCurves()
    {
        // Trail 头部先保持宽度，后半段再明显收尖
        AnimationCurve ghostCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.22f, 1f),
            new Keyframe(0.55f, 0.68f),
            new Keyframe(0.82f, 0.22f),
            new Keyframe(1f, 0f)
        );

        AnimationCurve mainCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.18f, 1f),
            new Keyframe(0.48f, 0.78f),
            new Keyframe(0.78f, 0.28f),
            new Keyframe(1f, 0f)
        );

        AnimationCurve coreCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.14f, 1f),
            new Keyframe(0.45f, 0.82f),
            new Keyframe(0.76f, 0.34f),
            new Keyframe(1f, 0f)
        );

        if (ghostTrail != null) ghostTrail.widthCurve = ghostCurve;
        if (mainTrail != null) mainTrail.widthCurve = mainCurve;
        if (coreTrail != null) coreTrail.widthCurve = coreCurve;
    }

    private void UpdateBoost()
    {
        if (boostTimer > 0f) boostTimer = Mathf.Max(0f, boostTimer - Time.deltaTime);
    }

    private void UpdateTrailFactor()
    {
        float targetFactor = Mathf.InverseLerp(minMomentum, 1f, motor.NormalizedMomentum);

        // Smoothstep，让低速保持干净，中高速增长更明显
        targetFactor = targetFactor * targetFactor * (3f - 2f * targetFactor);

        float responseSpeed = targetFactor > currentTrailFactor ? appearSpeed : disappearSpeed;
        currentTrailFactor = Mathf.MoveTowards(currentTrailFactor, targetFactor, responseSpeed * Time.deltaTime);
    }

    private void UpdateTrails()
    {
        bool shouldEmit = motor.NormalizedMomentum > minMomentum && motor.Speed > 0.2f;

        if (ghostTrail != null) ghostTrail.emitting = shouldEmit;
        if (mainTrail != null) mainTrail.emitting = shouldEmit;
        if (coreTrail != null) coreTrail.emitting = shouldEmit;

        float boost01 = boostDuration <= 0f ? 0f : Mathf.Clamp01(boostTimer / boostDuration);

        // Boost 开头非常强，然后快速衰减
        boost01 *= boost01;

        float lengthMultiplier = Mathf.Lerp(1f, boostLengthMultiplier, boost01);
        float widthMultiplier = Mathf.Lerp(1f, boostWidthMultiplier, boost01);
        float opacityMultiplier = Mathf.Lerp(1f, boostOpacityMultiplier, boost01);

        UpdateTrail(
            ghostTrail,
            ghostBlock,
            ghostMinTime,
            ghostMaxTime,
            ghostMinWidth,
            ghostMaxWidth,
            ghostMinOpacity,
            ghostMaxOpacity,
            ghostBoostStrength,
            lengthMultiplier,
            widthMultiplier,
            opacityMultiplier,
            boost01
        );

        UpdateTrail(
            mainTrail,
            mainBlock,
            mainMinTime,
            mainMaxTime,
            mainMinWidth,
            mainMaxWidth,
            mainMinOpacity,
            mainMaxOpacity,
            mainBoostStrength,
            lengthMultiplier,
            widthMultiplier,
            opacityMultiplier,
            boost01
        );

        UpdateTrail(
            coreTrail,
            coreBlock,
            coreMinTime,
            coreMaxTime,
            coreMinWidth,
            coreMaxWidth,
            coreMinOpacity,
            coreMaxOpacity,
            coreBoostStrength,
            lengthMultiplier,
            widthMultiplier,
            opacityMultiplier,
            boost01
        );
    }

    private void UpdateTrail(
        TrailRenderer trail,
        MaterialPropertyBlock block,
        float minTime,
        float maxTime,
        float minWidth,
        float maxWidth,
        float minOpacity,
        float maxOpacity,
        float boostStrength,
        float lengthMultiplier,
        float widthMultiplier,
        float opacityMultiplier,
        float boost01)
    {
        if (trail == null) return;

        float time = Mathf.Lerp(minTime, maxTime, currentTrailFactor);
        float width = Mathf.Lerp(minWidth, maxWidth, currentTrailFactor);
        float opacity = Mathf.Lerp(minOpacity, maxOpacity, currentTrailFactor);

        trail.time = time * lengthMultiplier;
        trail.widthMultiplier = width * widthMultiplier;

        trail.GetPropertyBlock(block);

        block.SetFloat(OpacityID, Mathf.Clamp01(opacity * opacityMultiplier));
        block.SetFloat(BoostID, boost01);
        block.SetFloat(BoostIntensityID, boostStrength);

        trail.SetPropertyBlock(block);
    }

    private void OnBoosted()
    {
        boostTimer = boostDuration;
    }

    public void ClearTrails()
    {
        if (ghostTrail != null) ghostTrail.Clear();
        if (mainTrail != null) mainTrail.Clear();
        if (coreTrail != null) coreTrail.Clear();
    }
}