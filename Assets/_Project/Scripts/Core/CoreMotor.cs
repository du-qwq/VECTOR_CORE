using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CoreMotor : MonoBehaviour
{
    [Header("基础移动")]
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float cruiseSpeed = 10f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float steeringStrength = 4f;

    [Header("阻力")]
    [SerializeField] private float movingDrag = 0.1f;
    [SerializeField] private float idleDrag = 0.35f;

    [Header("墙体反弹")]
    [Range(0f, 1f)] [SerializeField] private float wallBounceRetention = 0.88f;
    [SerializeField] private float wallControlLockTime = 0.16f;

    [Header("Boost")]
    [SerializeField] private float boostImpulse = 5f;
    [SerializeField] private float boostCooldown = 1f;

    [Header("Guard移动")]
    [Range(0f, 1f)] [SerializeField] private float guardAccelerationMultiplier = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float guardSteeringMultiplier = 0.6f;
    [SerializeField] private float guardBrake = 12f;

    [Header("Momentum")]
    [Range(0f, 1f)] [SerializeField] private float highMomentumThreshold = 0.72f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 velocityBeforePhysics;
    private Vector2 lastWallNormal;
    private float wallControlUnlockTime;
    private float nextBoostTime;
    private float controlLockedUntil;
    private bool touchingWall;
    private bool guarding;

    public event System.Action<Vector2, float> WallImpacted;
    public event System.Action Boosted;

    public Vector2 Velocity => rb.velocity;
    public Vector2 MoveInput => moveInput;
    public bool IsGuarding => guarding;
    public Vector2 PreCollisionVelocity => velocityBeforePhysics;
    public float Speed => rb.velocity.magnitude;
    public float NormalizedMomentum => Mathf.Clamp01(Speed / maxSpeed);
    public bool IsHighMomentum => NormalizedMomentum >= highMomentumThreshold;
    public bool CanBoost => Time.time >= nextBoostTime;
    public bool IsControlLocked => Time.time < controlLockedUntil;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void FixedUpdate()
    {
        Vector2 effectiveInput = IsControlLocked ? Vector2.zero : GetEffectiveInput();
        rb.drag = effectiveInput.sqrMagnitude > 0.01f ? movingDrag : idleDrag;

        if (effectiveInput.sqrMagnitude > 0.01f)
        {
            float currentAcceleration = guarding ? acceleration * guardAccelerationMultiplier : acceleration;
            float currentSteering = guarding ? steeringStrength * guardSteeringMultiplier : steeringStrength;

            if (Speed < cruiseSpeed) rb.AddForce(effectiveInput * currentAcceleration, ForceMode2D.Force);
            ApplySteering(effectiveInput, currentSteering);
        }

        if (guarding) rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, guardBrake * Time.fixedDeltaTime);

        ClampSpeed();
        velocityBeforePhysics = rb.velocity;
    }

    public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);

    public void RequestBoost() => TryBoost();

    public void SetGuarding(bool value) => guarding = value;

    public void ApplyImpulse(Vector2 impulse) => rb.AddForce(impulse, ForceMode2D.Impulse);

    public void ScaleVelocity(float multiplier)
    {
        rb.velocity *= Mathf.Max(0f, multiplier);
    }

    public void ApplyCoreCollisionVelocity(Vector2 velocity, float controlLockTime)
    {
        rb.velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        controlLockedUntil = Mathf.Max(controlLockedUntil, Time.time + controlLockTime);
    }

    private Vector2 GetEffectiveInput()
    {
        Vector2 input = moveInput;
        if (!touchingWall && Time.time >= wallControlUnlockTime) return input;

        float intoWall = Vector2.Dot(input, lastWallNormal);
        if (intoWall < 0f) input -= lastWallNormal * intoWall;

        return input.sqrMagnitude > 0.001f ? input.normalized : Vector2.zero;
    }

    private void ApplySteering(Vector2 direction, float strength)
    {
        if (Speed < 0.1f) return;

        Vector2 targetVelocity = direction * Speed;
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, strength * Time.fixedDeltaTime);
    }

    private void TryBoost()
    {
        if (!CanBoost || IsControlLocked || guarding) return;

        Vector2 boostDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : rb.velocity.normalized;

        if (touchingWall || Time.time < wallControlUnlockTime)
        {
            float intoWall = Vector2.Dot(boostDirection, lastWallNormal);
            if (intoWall < 0f) boostDirection -= lastWallNormal * intoWall;
        }

        if (boostDirection.sqrMagnitude < 0.01f) return;

        rb.AddForce(boostDirection.normalized * boostImpulse, ForceMode2D.Impulse);
        ClampSpeed();
        nextBoostTime = Time.time + boostCooldown;

        Boosted?.Invoke();
    }

    private void ClampSpeed()
    {
        if (Speed <= maxSpeed) return;
        rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wall") || collision.contactCount == 0) return;

        lastWallNormal = collision.GetContact(0).normal;
        touchingWall = true;
        wallControlUnlockTime = Time.time + wallControlLockTime;

        float impactSpeed = Mathf.Max(0f, -Vector2.Dot(velocityBeforePhysics, lastWallNormal));
        if (impactSpeed <= 0f) return;

        rb.velocity = Vector2.Reflect(velocityBeforePhysics, lastWallNormal) * wallBounceRetention;

        WallImpacted?.Invoke(lastWallNormal, impactSpeed);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wall") || collision.contactCount == 0) return;

        touchingWall = true;
        lastWallNormal = collision.GetContact(0).normal;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall")) touchingWall = false;
    }
    public void SteerVelocityTowards(Vector2 direction, float maxDegrees)
    {
        if (direction.sqrMagnitude < 0.001f || Speed < 0.1f) return;

        Vector2 currentDirection = rb.velocity.normalized;
        Vector2 targetDirection = direction.normalized;

        float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;

        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxDegrees);
        float radians = newAngle * Mathf.Deg2Rad;

        rb.velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * Speed;
    }
}