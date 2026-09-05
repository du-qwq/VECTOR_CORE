using UnityEngine;

[RequireComponent(typeof(CoreMotor))]
public class PlayerInputController : MonoBehaviour
{
    private CoreMotor motor;

    private void Awake() => motor = GetComponent<CoreMotor>();

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        motor.SetMoveInput(input);
        if (Input.GetKeyDown(KeyCode.LeftShift)) motor.RequestBoost();
    }
}