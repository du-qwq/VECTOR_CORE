using System.Collections.Generic;
using UnityEngine;

public static class ImpactResolver
{
    private static readonly Dictionary<ulong, float> lastImpactTimes = new Dictionary<ulong, float>();

    private const float ImpactCooldown = 0.25f;
    private const float MinClosingSpeed = 2.5f;
    private const float CoreRestitution = 0.85f;
    private const float NormalControlLock = 0.12f;
    private const float GuardKnockbackMultiplier = 0.35f;
    private const float GuardAttackerRebound = 0.35f;
    private const float PerfectReflectMultiplier = 0.9f;

    public static void Resolve(CoreCombat a, CoreCombat b, Collision2D collision)
    {
        if (collision.contactCount == 0) return;

        ulong pairKey = GetPairKey(a.GetInstanceID(), b.GetInstanceID());
        if (lastImpactTimes.TryGetValue(pairKey, out float lastTime) && Time.time - lastTime < ImpactCooldown) return;

        Vector2 normal = ((Vector2)b.transform.position - (Vector2)a.transform.position).normalized;
        Vector2 velocityA = a.Motor.PreCollisionVelocity;
        Vector2 velocityB = b.Motor.PreCollisionVelocity;

        float velocityANormal = Vector2.Dot(velocityA, normal);
        float velocityBNormal = Vector2.Dot(velocityB, normal);
        float closingSpeed = velocityANormal - velocityBNormal;
        if (closingSpeed < MinClosingSpeed) return;

        float attackSpeedA = Mathf.Max(0f, velocityANormal);
        float attackSpeedB = Mathf.Max(0f, -velocityBNormal);

        float baseDamageToB = a.CalculateDamage(attackSpeedA);
        float baseDamageToA = b.CalculateDamage(attackSpeedB);
        if (baseDamageToA <= 0f && baseDamageToB <= 0f) return;

        ReactionDefinition reactionA = baseDamageToB > 0f ? a.GetLoadedReaction() : null;
        ReactionDefinition reactionB = baseDamageToA > 0f ? b.GetLoadedReaction() : null;

        float rawCoreDamageToB = baseDamageToB * (reactionA != null ? reactionA.CoreDamageMultiplier : 1f);
        float rawGuardDamageToB = baseDamageToB * (reactionA != null ? reactionA.GuardDamageMultiplier : 1f);

        float rawCoreDamageToA = baseDamageToA * (reactionB != null ? reactionB.CoreDamageMultiplier : 1f);
        float rawGuardDamageToA = baseDamageToA * (reactionB != null ? reactionB.GuardDamageMultiplier : 1f);

        bool guardingA = a.Guard.IsGuarding;
        bool guardingB = b.Guard.IsGuarding;
        bool perfectA = guardingA && a.Guard.IsPerfectGuard && baseDamageToA > 0f;
        bool perfectB = guardingB && b.Guard.IsPerfectGuard && baseDamageToB > 0f;

        float damageToA = a.Guard.ResolveIncomingImpact(rawCoreDamageToA, rawGuardDamageToA, perfectA);
        float damageToB = b.Guard.ResolveIncomingImpact(rawCoreDamageToB, rawGuardDamageToB, perfectB);

        lastImpactTimes[pairKey] = Time.time;

        if (damageToA > 0f) a.Health.TakeDamage(damageToA);
        if (damageToB > 0f) b.Health.TakeDamage(damageToB);

        if (reactionA != null)
        {
            Debug.Log($"{a.name} → {reactionA.ReactionName}");
            a.ConsumeReaction(reactionA);
        }

        if (reactionB != null)
        {
            Debug.Log($"{b.name} → {reactionB.ReactionName}");
            b.ConsumeReaction(reactionB);
        }

        ResolveVelocity(a, b, velocityA, velocityB, normal, velocityANormal, velocityBNormal, attackSpeedA, attackSpeedB, guardingA, guardingB, perfectA, perfectB);

        if (perfectA) Debug.Log($"{a.name} PERFECT GUARD");
        if (perfectB) Debug.Log($"{b.name} PERFECT GUARD");
    }

    private static void ResolveVelocity(CoreCombat a, CoreCombat b, Vector2 velocityA, Vector2 velocityB, Vector2 normal, float velocityANormal, float velocityBNormal, float attackSpeedA, float attackSpeedB, bool guardingA, bool guardingB, bool perfectA, bool perfectB)
    {
        Vector2 tangentA = velocityA - normal * velocityANormal;
        Vector2 tangentB = velocityB - normal * velocityBNormal;

        float newANormal = ((1f - CoreRestitution) * velocityANormal + (1f + CoreRestitution) * velocityBNormal) * 0.5f;
        float newBNormal = ((1f + CoreRestitution) * velocityANormal + (1f - CoreRestitution) * velocityBNormal) * 0.5f;

        Vector2 newA = tangentA + normal * newANormal;
        Vector2 newB = tangentB + normal * newBNormal;

        if (perfectA && perfectB)
        {
            newA = tangentA * 0.4f - normal * Mathf.Max(attackSpeedA * PerfectReflectMultiplier, 4f);
            newB = tangentB * 0.4f + normal * Mathf.Max(attackSpeedB * PerfectReflectMultiplier, 4f);
        }
        else if (perfectA)
        {
            newA = velocityA * 0.2f;
            newB = tangentB + normal * Mathf.Max(attackSpeedB * PerfectReflectMultiplier, 4f);
        }
        else if (perfectB)
        {
            newB = velocityB * 0.2f;
            newA = tangentA - normal * Mathf.Max(attackSpeedA * PerfectReflectMultiplier, 4f);
        }
        else
        {
            if (guardingA && attackSpeedB > 0f)
            {
                newA = Vector2.Lerp(velocityA, newA, GuardKnockbackMultiplier);
                newB = Vector2.Lerp(newB, tangentB + normal * attackSpeedB * GuardAttackerRebound, 0.5f);
            }

            if (guardingB && attackSpeedA > 0f)
            {
                newB = Vector2.Lerp(velocityB, newB, GuardKnockbackMultiplier);
                newA = Vector2.Lerp(newA, tangentA - normal * attackSpeedA * GuardAttackerRebound, 0.5f);
            }
        }

        float lockA = perfectB ? 0.18f : NormalControlLock;
        float lockB = perfectA ? 0.18f : NormalControlLock;

        a.Motor.ApplyCoreCollisionVelocity(newA, lockA);
        b.Motor.ApplyCoreCollisionVelocity(newB, lockB);
    }

    private static ulong GetPairKey(int a, int b)
    {
        uint min = (uint)Mathf.Min(a, b);
        uint max = (uint)Mathf.Max(a, b);
        return ((ulong)min << 32) | max;
    }
}