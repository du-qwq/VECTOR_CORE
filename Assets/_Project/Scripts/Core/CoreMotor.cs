using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CoreMotor : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float steeringStrength = 4f;

    [Header("阻力")]
    [SerializeField] private float movingDrag = 0.1f;
    [SerializeField] private float idleDrag = 0.35f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    public Vector2 Velocity => rb.velocity;
    public float Speed => rb.velocity.magnitude;
    public float NormalizedMomentum => Mathf.Clamp01(Speed / maxSpeed);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    private void FixedUpdate()
    {
        rb.drag = moveInput.sqrMagnitude > 0.01f ? movingDrag : idleDrag;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.AddForce(moveInput * acceleration, ForceMode2D.Force);
            ApplySteering();
        }

        ClampSpeed();
    }

    private void ApplySteering()
    {
        if (Speed < 0.1f) return;
        Vector2 targetVelocity = moveInput * Speed;
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, steeringStrength * Time.fixedDeltaTime);
    }

    private void ClampSpeed()
    {
        if (Speed <= maxSpeed) return;
        rb.velocity = rb.velocity.normalized * maxSpeed;
    }
}