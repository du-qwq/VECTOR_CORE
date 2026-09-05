using System.Collections.Generic;
using UnityEngine;

public static class ImpactResolver
{
    private static readonly Dictionary<ulong, float> lastImpactTimes = new Dictionary<ulong, float>();

    private const float ImpactCooldown = 0.25f;
    private const float MinClosingSpeed = 2.5f;
    private const float CoreRestitution = 0.85f;
    private const float ImpactControlLockTime = 0.12f;

    public static void Resolve(CoreCombat a, CoreCombat b, Collision2D collision)
    {
        if (collision.contactCount == 0) return;

        ulong pairKey = GetPairKey(a.GetInstanceID(), b.GetInstanceID());
        if (lastImpactTimes.TryGetValue(pairKey, out float lastTime) && Time.time - lastTime < ImpactCooldown) return;

        Vector2 directionAB = ((Vector2)b.transform.position - (Vector2)a.transform.position).normalized;
        Vector2 velocityA = a.Motor.PreCollisionVelocity;
        Vector2 velocityB = b.Motor.PreCollisionVelocity;

        float velocityANormal = Vector2.Dot(velocityA, directionAB);
        float velocityBNormal = Vector2.Dot(velocityB, directionAB);
        float closingSpeed = velocityANormal - velocityBNormal;

        if (closingSpeed < MinClosingSpeed) return;

        float attackSpeedA = Mathf.Max(0f, velocityANormal);
        float attackSpeedB = Mathf.Max(0f, -velocityBNormal);

        float damageToB = a.CalculateDamage(attackSpeedA);
        float damageToA = b.CalculateDamage(attackSpeedB);

        if (damageToA <= 0f && damageToB <= 0f) return;

        lastImpactTimes[pairKey] = Time.time;

        if (damageToB > 0f) b.Health.TakeDamage(damageToB);
        if (damageToA > 0f) a.Health.TakeDamage(damageToA);

        ResolveVelocity(a, b, velocityA, velocityB, directionAB, velocityANormal, velocityBNormal);
    }

    private static void ResolveVelocity(CoreCombat a, CoreCombat b, Vector2 velocityA, Vector2 velocityB, Vector2 normal, float velocityANormal, float velocityBNormal)
    {
        Vector2 velocityATangent = velocityA - normal * velocityANormal;
        Vector2 velocityBTangent = velocityB - normal * velocityBNormal;

        float newVelocityANormal = ((1f - CoreRestitution) * velocityANormal + (1f + CoreRestitution) * velocityBNormal) * 0.5f;
        float newVelocityBNormal = ((1f + CoreRestitution) * velocityANormal + (1f - CoreRestitution) * velocityBNormal) * 0.5f;

        Vector2 newVelocityA = velocityATangent + normal * newVelocityANormal;
        Vector2 newVelocityB = velocityBTangent + normal * newVelocityBNormal;

        a.Motor.ApplyCoreCollisionVelocity(newVelocityA, ImpactControlLockTime);
        b.Motor.ApplyCoreCollisionVelocity(newVelocityB, ImpactControlLockTime);
    }

    private static ulong GetPairKey(int a, int b)
    {
        uint min = (uint)Mathf.Min(a, b);
        uint max = (uint)Mathf.Max(a, b);
        return ((ulong)min << 32) | max;
    }
}