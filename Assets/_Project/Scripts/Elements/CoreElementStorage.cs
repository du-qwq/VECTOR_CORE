using UnityEngine;

public class CoreElementStorage : MonoBehaviour
{
    [SerializeField] private ElementType slotA = ElementType.None;
    [SerializeField] private ElementType slotB = ElementType.None;

    public ElementType SlotA => slotA;
    public ElementType SlotB => slotB;
    public bool IsFull => slotA != ElementType.None && slotB != ElementType.None;
    public bool IsEmpty => slotA == ElementType.None && slotB == ElementType.None;

    public bool AddElement(ElementType element)
    {
        if (element == ElementType.None) return false;

        if (slotA == ElementType.None)
        {
            slotA = element;
            Debug.Log($"{name} SLOT A：{element}");
            return true;
        }

        if (slotB == ElementType.None)
        {
            slotB = element;
            Debug.Log($"{name} SLOT B：{element}");
            return true;
        }

        return false;
    }

    public void Clear()
    {
        slotA = ElementType.None;
        slotB = ElementType.None;
    }
}