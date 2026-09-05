using UnityEngine;

public class CoreGuardVisual : MonoBehaviour
{
    [SerializeField] private CoreGuard guard;
    [SerializeField] private GameObject guardRing;
    [SerializeField] private float normalScale = 1.35f;
    [SerializeField] private float perfectScale = 1.65f;
    [SerializeField] private float pulseSpeed = 12f;

    private void Update()
    {
        bool active = guard.IsGuarding;
        guardRing.SetActive(active);
        if (!active) return;

        float targetScale = guard.IsPerfectGuard ? perfectScale : normalScale;
        float pulse = guard.IsPerfectGuard ? Mathf.Sin(Time.time * pulseSpeed) * 0.08f : 0f;
        guardRing.transform.localScale = Vector3.one * (targetScale + pulse);
    }
}