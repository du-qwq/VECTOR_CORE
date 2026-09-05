using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
[RequireComponent(typeof(CoreGuard))]
public class CoreStatusController : MonoBehaviour
{
    [Header("导电")]
    [SerializeField] private float conductiveDuration = 2.5f;
    [SerializeField] private float conductiveWallMinSpeed = 3f;
    [SerializeField] private float conductiveWallGuardDamage = 35f;
    [Range(0f, 1f)] [SerializeField] private float conductiveWallMomentumRetention = 0.45f;

    [Header("临时视觉")]
    [SerializeField] private GameObject conductiveVisual;

    private CoreMotor motor;
    private CoreGuard guard;
    private float conductiveUntil;

    public bool IsConductive => Time.time < conductiveUntil;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        guard = GetComponent<CoreGuard>();
    }

    private void OnEnable()
    {
        if (motor == null) motor = GetComponent<CoreMotor>();
        motor.WallImpacted += OnWallImpacted;
    }

    private void OnDisable()
    {
        if (motor != null) motor.WallImpacted -= OnWallImpacted;
    }

    private void Update()
    {
        if (conductiveVisual != null) conductiveVisual.SetActive(IsConductive);
    }

    public void ApplyConductive()
    {
        conductiveUntil = Time.time + conductiveDuration;
        Debug.Log($"{name} CONDUCTIVE");
    }

    private void OnWallImpacted(Vector2 normal, float impactSpeed)
    {
        if (!IsConductive || impactSpeed < conductiveWallMinSpeed) return;

        guard.DamageGuard(conductiveWallGuardDamage);
        motor.ScaleVelocity(conductiveWallMomentumRetention);
        conductiveUntil = 0f;

        Debug.Log($"{name} CONDUCTION DISCHARGE");
    }
}