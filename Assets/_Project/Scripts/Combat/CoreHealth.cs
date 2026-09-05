using UnityEngine;

public class CoreHealth : MonoBehaviour
{
    [SerializeField] private float maxCore = 100f;
    [SerializeField] private float currentCore;

    public float MaxCore => maxCore;
    public float CurrentCore => currentCore;
    public float NormalizedCore => maxCore <= 0f ? 0f : currentCore / maxCore;
    public bool IsDead => currentCore <= 0f;

    private void Awake() => currentCore = maxCore;

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead) return;
        currentCore = Mathf.Max(0f, currentCore - damage);
        Debug.Log($"{name} 受到 {damage:F1} 伤害，CORE：{currentCore:F1}/{maxCore}");
        if (IsDead) Debug.Log($"{name} CORE DESTROYED");
    }

    public void ResetCore() => currentCore = maxCore;
}