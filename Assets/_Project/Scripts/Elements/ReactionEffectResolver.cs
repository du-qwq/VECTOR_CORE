public static class ReactionEffectResolver
{
    public static void Apply(CoreCombat attacker, CoreCombat defender, ReactionDefinition reaction)
    {
        if (reaction == null) return;

        switch (reaction.ReactionType)
        {
            case ReactionType.Conduction:
                defender.Status.ApplyConductive();
                break;
        }
    }
}