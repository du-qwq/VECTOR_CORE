using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CoreCameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform target;
    [SerializeField] private CoreMotor targetMotor;

    [Header("基础跟随")]
    [SerializeField] private float followSmoothTime = 0.12f;

    [Header("速度前视")]
    [SerializeField] private float lookAheadDistance = 2.2f;
    [SerializeField] private float lookAheadSmoothTime = 0.16f;
    [SerializeField] private float minLookAheadSpeed = 1f;

    [Header("场地限制")]
    [SerializeField] private bool clampToArena = true;
    [SerializeField] private Vector2 arenaCenter = Vector2.zero;
    [SerializeField] private Vector2 arenaSize = new Vector2(32f, 18f);
    [SerializeField] private float arenaPadding = 0.25f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeFrequency = 32f;

    private Camera cam;

    private Vector3 followVelocity;
    private Vector2 lookAheadVelocity;
    private Vector2 currentLookAhead;

    private float cameraZ;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;
    private float shakeSeed;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cameraZ = transform.position.z;
        shakeSeed = Random.Range(0f, 1000f);

        if (target != null && targetMotor == null) targetMotor = target.GetComponent<CoreMotor>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateLookAhead();

        Vector3 desiredPosition = new Vector3(
            target.position.x + currentLookAhead.x,
            target.position.y + currentLookAhead.y,
            cameraZ
        );

        if (clampToArena) desiredPosition = ClampToArena(desiredPosition);

        Vector3 smoothPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime
        );

        Vector2 shakeOffset = UpdateShake();

        transform.position = new Vector3(
            smoothPosition.x + shakeOffset.x,
            smoothPosition.y + shakeOffset.y,
            cameraZ
        );
    }

    private void UpdateLookAhead()
    {
        if (targetMotor == null)
        {
            currentLookAhead = Vector2.SmoothDamp(currentLookAhead, Vector2.zero, ref lookAheadVelocity, lookAheadSmoothTime);
            return;
        }

        Vector2 velocity = targetMotor.Velocity;
        Vector2 targetLookAhead = Vector2.zero;

        if (velocity.magnitude >= minLookAheadSpeed)
        {
            float momentum = targetMotor.NormalizedMomentum;
            targetLookAhead = velocity.normalized * lookAheadDistance * momentum;
        }

        currentLookAhead = Vector2.SmoothDamp(
            currentLookAhead,
            targetLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );
    }

    private Vector2 UpdateShake()
    {
        if (shakeTimer <= 0f)
        {
            shakeTimer = 0f;
            shakeStrength = 0f;
            return Vector2.zero;
        }

        shakeTimer -= Time.deltaTime;

        float normalizedTime = shakeDuration <= 0f ? 0f : Mathf.Clamp01(shakeTimer / shakeDuration);
        float fade = normalizedTime * normalizedTime;

        float time = Time.time * shakeFrequency;

        float noiseX = Mathf.PerlinNoise(shakeSeed, time) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(shakeSeed + 53.17f, time) * 2f - 1f;

        return new Vector2(noiseX, noiseY) * shakeStrength * fade;
    }

    private Vector3 ClampToArena(Vector3 position)
    {
        if (!cam.orthographic) return position;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector2 halfArena = arenaSize * 0.5f;

        float minX = arenaCenter.x - halfArena.x + halfWidth + arenaPadding;
        float maxX = arenaCenter.x + halfArena.x - halfWidth - arenaPadding;

        float minY = arenaCenter.y - halfArena.y + halfHeight + arenaPadding;
        float maxY = arenaCenter.y + halfArena.y - halfHeight - arenaPadding;

        if (minX <= maxX) position.x = Mathf.Clamp(position.x, minX, maxX);
        else position.x = arenaCenter.x;

        if (minY <= maxY) position.y = Mathf.Clamp(position.y, minY, maxY);
        else position.y = arenaCenter.y;

        return position;
    }

    public void Shake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;

        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetMotor = target != null ? target.GetComponent<CoreMotor>() : null;

        currentLookAhead = Vector2.zero;
        followVelocity = Vector3.zero;
        lookAheadVelocity = Vector2.zero;
    }
}