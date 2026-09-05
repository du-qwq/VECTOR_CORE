using UnityEngine;

[CreateAssetMenu(fileName = "ReactionDatabase", menuName = "VECTOR CORE/Reaction Database")]
public class ElementReactionDatabase : ScriptableObject
{
    [SerializeField] private ReactionDefinition[] reactions;

    public ReactionDefinition Find(ElementType a, ElementType b)
    {
        if (a == ElementType.None || b == ElementType.None) return null;

        foreach (ReactionDefinition reaction in reactions)
        {
            if (reaction == null) continue;
            bool forward = reaction.ElementA == a && reaction.ElementB == b;
            bool backward = reaction.ElementA == b && reaction.ElementB == a;
            if (forward || backward) return reaction;
        }

        return null;
    }
}