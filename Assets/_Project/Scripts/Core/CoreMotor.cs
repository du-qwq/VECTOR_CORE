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

    [Header("Momentum")]
    [Range(0f, 1f)] [SerializeField] private float highMomentumThreshold = 0.72f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 velocityBeforePhysics;
    private Vector2 lastWallNormal;
    private float wallControlUnlockTime;
    private float nextBoostTime;
    private bool touchingWall;

    public Vector2 Velocity => rb.velocity;
    public float Speed => rb.velocity.magnitude;
    public float NormalizedMomentum => Mathf.Clamp01(Speed / maxSpeed);
    public bool IsHighMomentum => NormalizedMomentum >= highMomentumThreshold;
    public bool CanBoost => Time.time >= nextBoostTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (Input.GetKeyDown(KeyCode.LeftShift)) TryBoost();
    }

    private void FixedUpdate()
    {
        Vector2 effectiveInput = GetEffectiveInput();

        rb.drag = moveInput.sqrMagnitude > 0.01f ? movingDrag : idleDrag;

        if (effectiveInput.sqrMagnitude > 0.01f)
        {
            if (Speed < cruiseSpeed) rb.AddForce(effectiveInput * acceleration, ForceMode2D.Force);
            ApplySteering(effectiveInput);
        }

        ClampSpeed();
        velocityBeforePhysics = rb.velocity;
    }

    private Vector2 GetEffectiveInput()
    {
        Vector2 input = moveInput;
        if (!touchingWall && Time.time >= wallControlUnlockTime) return input;

        float intoWall = Vector2.Dot(input, lastWallNormal);
        if (intoWall < 0f) input -= lastWallNormal * intoWall;

        return input.sqrMagnitude > 0.001f ? input.normalized : Vector2.zero;
    }

    private void ApplySteering(Vector2 direction)
    {
        if (Speed < 0.1f) return;
        Vector2 targetVelocity = direction * Speed;
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, steeringStrength * Time.fixedDeltaTime);
    }

    private void TryBoost()
    {
        if (!CanBoost) return;

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

        if (Vector2.Dot(velocityBeforePhysics, lastWallNormal) >= 0f) return;
        rb.velocity = Vector2.Reflect(velocityBeforePhysics, lastWallNormal) * wallBounceRetention;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wall") || collision.contactCount == 0) return;
        touchingWall = true;
        lastWallNormal = collision.GetContact(0).normal;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wall")) return;
        touchingWall = false;
    }
}