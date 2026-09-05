using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
public class SimpleChaseAI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float boostDistance = 5f;
    [SerializeField] private float boostInterval = 1.5f;

    private CoreMotor motor;
    private float nextBoostTime;

    private void Awake() => motor = GetComponent<CoreMotor>();

    private void Update()
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        motor.SetMoveInput(direction);

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= boostDistance && Time.time >= nextBoostTime)
        {
            motor.RequestBoost();
            nextBoostTime = Time.time + boostInterval;
        }
    }
}