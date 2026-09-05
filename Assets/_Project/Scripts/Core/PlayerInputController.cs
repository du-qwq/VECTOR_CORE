using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
[RequireComponent(typeof(CoreGuard))]
public class PlayerInputController : MonoBehaviour
{
    private CoreMotor motor;
    private CoreGuard guard;

    private void Awake()
    {
        motor = GetComponent<CoreMotor>();
        guard = GetComponent<CoreGuard>();
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        motor.SetMoveInput(input);

        if (Input.GetKeyDown(KeyCode.LeftShift)) motor.RequestBoost();
        guard.SetGuardInput(Input.GetKey(KeyCode.Space));
    }
}