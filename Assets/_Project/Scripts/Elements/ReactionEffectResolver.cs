public static class ReactionEffectResolver
{
    private const float StabilizeGuardRestore = 30f;

    public static void Apply(CoreCombat attacker, CoreCombat defender, ReactionDefinition reaction)
    {
        if (reaction == null) return;

        switch (reaction.ReactionType)
        {
            case ReactionType.Stabilize:
                attacker.Guard.RestoreGuard(StabilizeGuardRestore);
                break;

            case ReactionType.Conduction:
                defender.Status.ApplyConductive();
                break;
        }
    }
}