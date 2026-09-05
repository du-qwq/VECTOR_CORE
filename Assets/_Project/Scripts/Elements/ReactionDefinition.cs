using UnityEngine;

[CreateAssetMenu(fileName = "Reaction_", menuName = "VECTOR CORE/Reaction Definition")]
public class ReactionDefinition : ScriptableObject
{
    [Header("基本信息")]
    [SerializeField] private string reactionName;
    [SerializeField] private ReactionType reactionType;
    [SerializeField] private ElementType elementA;
    [SerializeField] private ElementType elementB;

    [Header("碰撞倍率")]
    [SerializeField] private float coreDamageMultiplier = 1f;
    [SerializeField] private float guardDamageMultiplier = 1f;

    public string ReactionName => reactionName;
    public ReactionType ReactionType => reactionType;
    public ElementType ElementA => elementA;
    public ElementType ElementB => elementB;
    public float CoreDamageMultiplier => coreDamageMultiplier;
    public float GuardDamageMultiplier => guardDamageMultiplier;
}