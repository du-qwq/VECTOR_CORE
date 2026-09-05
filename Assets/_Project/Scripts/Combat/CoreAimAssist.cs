using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
public class CoreAimAssist : MonoBehaviour
{
    [Header("辅助条件")]
    [SerializeField] private float minAssistSpeed = 6f;
    [SerializeField] private float assistRadius = 3f;
    [SerializeField] private float assistAngle = 22f;

    [Header("转向辅助")]
    [SerializeField] private float minTurnSpeed = 20f;
    [SerializeField] private float maxTurnSpeed = 85f;

    [Header("输入限制")]
    [SerializeField] private bool requireMoveInput = true;
    [SerializeField] private float inputTargetMaxAngle = 35f;

    [Header("目标")]
    [SerializeField] private LayerMask targetMask = ~0;

    private readonly Collider2D[] results = new Collider2D[16];

    private CoreMotor motor;
    private CoreCombat selfCombat;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        selfCombat = GetComponent<CoreCombat>();
    }

    private void FixedUpdate()
    {
        if (motor.Speed < minAssistSpeed) return;
        if (motor.IsGuarding || motor.IsControlLocked) return;
        if (requireMoveInput && motor.MoveInput.sqrMagnitude < 0.01f) return;

        CoreCombat target = FindBestTarget();
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        if (distance < 0.001f) return;

        Vector2 targetDirection = toTarget / distance;

        float distanceFactor = 1f - Mathf.Clamp01(distance / assistRadius);
        float angle = Vector2.Angle(motor.Velocity.normalized, targetDirection);
        float angleFactor = 1f - Mathf.Clamp01(angle / assistAngle);

        float strength = distanceFactor * angleFactor;
        strength *= strength;

        float turnSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, strength);
        motor.SteerVelocityTowards(targetDirection, turnSpeed * Time.fixedDeltaTime);
    }

    private CoreCombat FindBestTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, assistRadius, results, targetMask);

        CoreCombat bestTarget = null;
        float bestScore = float.MinValue;

        Vector2 velocityDirection = motor.Velocity.normalized;
        Vector2 inputDirection = motor.MoveInput.sqrMagnitude > 0.01f ? motor.MoveInput.normalized : velocityDirection;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = results[i];

            if (hit == null) continue;

            CoreCombat candidate = hit.GetComponentInParent<CoreCombat>();

            if (candidate == null) continue;
            if (candidate == selfCombat) continue;

            Vector2 toTarget = (Vector2)candidate.transform.position - (Vector2)transform.position;

            float distance = toTarget.magnitude;
            if (distance <= 0.001f) continue;

            Vector2 targetDirection = toTarget / distance;

            float velocityAngle = Vector2.Angle(velocityDirection, targetDirection);
            if (velocityAngle > assistAngle) continue;

            if (requireMoveInput)
            {
                float inputAngle = Vector2.Angle(inputDirection, targetDirection);
                if (inputAngle > inputTargetMaxAngle) continue;
            }

            float alignmentScore = 1f - velocityAngle / assistAngle;
            float distanceScore = 1f - distance / assistRadius;

            float score = alignmentScore * 0.65f + distanceScore * 0.35f;

            if (score <= bestScore) continue;

            bestScore = score;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, assistRadius);
    }
}