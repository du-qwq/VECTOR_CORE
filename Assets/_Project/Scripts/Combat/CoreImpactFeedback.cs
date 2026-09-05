using UnityEngine;

public class CoreImpactFeedback : MonoBehaviour
{
    [Header("视觉对象")]
    [SerializeField] private Transform visualRoot;

    [Header("Camera")]
    [SerializeField] private CoreCameraFollow cameraFollow;

    [Header("触发条件")]
    [SerializeField] private float minImpactSpeed = 2.5f;
    [SerializeField] private float maxImpactSpeed = 15f;

    [Header("Core碰撞")]
    [SerializeField] private float coreSquashStrength = 0.18f;
    [SerializeField] private float coreSquashDuration = 0.13f;

    [Header("撞墙")]
    [SerializeField] private float wallSquashStrength = 0.11f;
    [SerializeField] private float wallSquashDuration = 0.10f;

    [Header("Camera Shake")]
    [SerializeField] private float minShakeStrength = 0.035f;
    [SerializeField] private float maxShakeStrength = 0.14f;
    [SerializeField] private float minShakeDuration = 0.06f;
    [SerializeField] private float maxShakeDuration = 0.13f;

    private Vector3 baseScale;

    private bool impactPlaying;
    private float impactTimer;
    private float impactDuration;
    private float impactStrength;
    private Vector2 impactNormal;

    private void Awake()
    {
        if (visualRoot != null) baseScale = visualRoot.localScale;
    }

    private void LateUpdate()
    {
        UpdateImpactAnimation();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0) return;

        bool hitWall = collision.collider.CompareTag("Wall");
        bool hitCore = collision.collider.GetComponentInParent<CoreCombat>() != null;

        if (!hitWall && !hitCore) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed) return;

        Vector2 normal = collision.GetContact(0).normal;

        float normalizedImpact = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, impactSpeed);
        normalizedImpact = Mathf.Clamp01(normalizedImpact);

        if (hitCore)
        {
            TriggerImpact(
                normal,
                coreSquashStrength * Mathf.Lerp(0.55f, 1f, normalizedImpact),
                coreSquashDuration
            );

            TriggerCameraShake(normalizedImpact, 1f);
        }
        else if (hitWall)
        {
            TriggerImpact(
                normal,
                wallSquashStrength * Mathf.Lerp(0.45f, 1f, normalizedImpact),
                wallSquashDuration
            );

            TriggerCameraShake(normalizedImpact, 0.65f);
        }
    }

    private void TriggerImpact(Vector2 normal, float strength, float duration)
    {
        if (visualRoot == null) return;

        impactNormal = normal.normalized;
        impactStrength = strength;
        impactDuration = duration;
        impactTimer = duration;
        impactPlaying = true;
    }

    private void TriggerCameraShake(float impact01, float multiplier)
    {
        if (cameraFollow == null) return;

        float strength = Mathf.Lerp(minShakeStrength, maxShakeStrength, impact01) * multiplier;
        float duration = Mathf.Lerp(minShakeDuration, maxShakeDuration, impact01);

        cameraFollow.Shake(strength, duration);
    }

    private void UpdateImpactAnimation()
    {
        if (visualRoot == null) return;

        if (!impactPlaying)
        {
            visualRoot.localScale = baseScale;
            return;
        }

        impactTimer -= Time.deltaTime;

        float phase = impactDuration <= 0f ? 1f : 1f - Mathf.Clamp01(impactTimer / impactDuration);

        // 前半段压缩，后半段回弹
        float pulse = Mathf.Sin(phase * Mathf.PI);

        float xInfluence = Mathf.Abs(impactNormal.x);
        float yInfluence = Mathf.Abs(impactNormal.y);

        float compressX = 1f - impactStrength * xInfluence * pulse;
        float compressY = 1f - impactStrength * yInfluence * pulse;

        float expandX = 1f + impactStrength * 0.45f * yInfluence * pulse;
        float expandY = 1f + impactStrength * 0.45f * xInfluence * pulse;

        Vector3 targetScale = new Vector3(
            baseScale.x * compressX * expandX,
            baseScale.y * compressY * expandY,
            baseScale.z
        );

        visualRoot.localScale = targetScale;

        if (impactTimer > 0f) return;

        impactPlaying = false;
        visualRoot.localScale = baseScale;
    }
}