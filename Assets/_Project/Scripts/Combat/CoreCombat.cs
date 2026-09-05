using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
[RequireComponent(typeof(CoreHealth))]
[RequireComponent(typeof(CoreGuard))]
[RequireComponent(typeof(CoreElementStorage))]
[RequireComponent(typeof(CoreStatusController))]
public class CoreCombat : MonoBehaviour
{
    [Header("碰撞伤害")]
    [SerializeField] private float minAttackSpeed = 2.5f;
    [SerializeField] private float damagePerSpeed = 4f;

    [Header("元素反应")]
    [SerializeField] private ElementReactionDatabase reactionDatabase;

    private CoreMotor motor;
    private CoreHealth health;
    private CoreGuard guard;
    private CoreElementStorage elementStorage;
    private CoreStatusController status;

    public CoreMotor Motor => motor;
    public CoreHealth Health => health;
    public CoreGuard Guard => guard;
    public CoreElementStorage ElementStorage => elementStorage;
    public CoreStatusController Status => status;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        health = GetComponent<CoreHealth>();
        guard = GetComponent<CoreGuard>();
        elementStorage = GetComponent<CoreElementStorage>();
        status = GetComponent<CoreStatusController>();
    }

    public float CalculateDamage(float attackSpeed)
    {
        if (attackSpeed < minAttackSpeed) return 0f;
        return (attackSpeed - minAttackSpeed) * damagePerSpeed;
    }

    public ReactionDefinition GetLoadedReaction()
    {
        if (reactionDatabase == null || !elementStorage.IsFull) return null;
        return reactionDatabase.Find(elementStorage.SlotA, elementStorage.SlotB);
    }

    public void ConsumeReaction(ReactionDefinition reaction)
    {
        if (reaction == null) return;
        elementStorage.Clear();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CoreCombat other = collision.collider.GetComponent<CoreCombat>();
        if (other == null || other == this) return;

        // 两个 Core 都会收到 OnCollisionEnter2D，只让其中一个负责结算。
        if (GetInstanceID() > other.GetInstanceID()) return;

        ImpactResolver.Resolve(this, other, collision);
    }
}