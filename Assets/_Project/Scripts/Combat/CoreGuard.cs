using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
public class CoreGuard : MonoBehaviour
{
    [Header("Guard")]
    [SerializeField] private float maxGuard = 100f;
    [Range(0f, 1f)] [SerializeField] private float guardedCoreDamageMultiplier = 0.3f;
    [SerializeField] private float guardDamageMultiplier = 0.9f;

    [Header("Perfect Guard")]
    [SerializeField] private float perfectGuardWindow = 0.12f;
    [SerializeField] private float perfectGuardCostMultiplier = 0.35f;

    [Header("恢复")]
    [SerializeField] private float rechargeDelay = 1f;
    [SerializeField] private float rechargeRate = 25f;
    [SerializeField] private float guardBreakDuration = 1.2f;
    [Range(0f, 1f)] [SerializeField] private float guardBreakRecovery = 0.35f;

    private CoreMotor motor;
    private float currentGuard;
    private float guardStartTime;
    private float rechargeUnlockTime;
    private float brokenUntil;
    private bool guardInput;
    private bool broken;

    public float CurrentGuard => currentGuard;
    public float NormalizedGuard => currentGuard / maxGuard;
    public bool IsBroken => broken;
    public bool IsGuarding => guardInput && !broken;
    public bool IsPerfectGuard => IsGuarding && Time.time - guardStartTime <= perfectGuardWindow;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        currentGuard = maxGuard;
    }

    private void Update()
    {
        if (broken && Time.time >= brokenUntil)
        {
            broken = false;
            currentGuard = maxGuard * guardBreakRecovery;
        }

        if (!IsGuarding && !broken && Time.time >= rechargeUnlockTime) currentGuard = Mathf.MoveTowards(currentGuard, maxGuard, rechargeRate * Time.deltaTime);

        motor.SetGuarding(IsGuarding);
    }

    public void SetGuardInput(bool active)
    {
        if (active && !guardInput && !broken) guardStartTime = Time.time;
        guardInput = active;
    }

    public float ResolveIncomingDamage(float damage, bool perfect)
    {
        if (damage <= 0f) return 0f;
        if (!IsGuarding) return damage;

        ApplyGuardDamage(damage * (perfect ? perfectGuardCostMultiplier : guardDamageMultiplier));
        return perfect ? 0f : damage * guardedCoreDamageMultiplier;
    }

    private void ApplyGuardDamage(float damage)
    {
        currentGuard = Mathf.Max(0f, currentGuard - damage);
        rechargeUnlockTime = Time.time + rechargeDelay;

        if (currentGuard > 0f) return;

        broken = true;
        guardInput = false;
        brokenUntil = Time.time + guardBreakDuration;
        motor.SetGuarding(false);
        Debug.Log($"{name} GUARD BREAK");
    }
}