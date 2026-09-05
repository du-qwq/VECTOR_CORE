using UnityEngine;

public class ElementNode : MonoBehaviour
{
    [SerializeField] private ElementType elementType;

    public ElementType ElementType => elementType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        CoreElementStorage storage = other.GetComponent<CoreElementStorage>();
        if (storage == null) return;
        if (!storage.AddElement(elementType)) return;

        Debug.Log($"{other.name} 获取 {elementType}");
        Destroy(gameObject);
    }
}