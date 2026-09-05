using UnityEngine;

public class CorePrototypeVisual : MonoBehaviour
{
    [SerializeField] private CoreMotor motor;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float minTrailTime = 0.05f;
    [SerializeField] private float maxTrailTime = 0.4f;
    [SerializeField] private float minWidth = 0.05f;
    [SerializeField] private float maxWidth = 0.22f;

    private void Update()
    {
        float momentum = motor.NormalizedMomentum;
        trail.time = Mathf.Lerp(minTrailTime, maxTrailTime, momentum);
        trail.startWidth = Mathf.Lerp(minWidth, maxWidth, momentum);
    }
}