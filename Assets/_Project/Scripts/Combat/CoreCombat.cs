using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
[RequireComponent(typeof(CoreHealth))]
public class CoreCombat : MonoBehaviour
{
    [Header("碰撞伤害")]
    [SerializeField] private float minAttackSpeed = 2.5f;
    [SerializeField] private float damagePerSpeed = 4f;

    private CoreMotor motor;
    private CoreHealth health;

    public CoreMotor Motor => motor;
    public CoreHealth Health => health;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        health = GetComponent<CoreHealth>();
    }

    public float CalculateDamage(float attackSpeed)
    {
        if (attackSpeed < minAttackSpeed) return 0f;
        return (attackSpeed - minAttackSpeed) * damagePerSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CoreCombat other = collision.collider.GetComponent<CoreCombat>();
        if (other == null || other == this) return;
        if (GetInstanceID() > other.GetInstanceID()) return;

        ImpactResolver.Resolve(this, other, collision);
    }
}