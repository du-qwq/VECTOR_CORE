using UnityEngine;

public class ElementSpawnPoint : MonoBehaviour
{
    [Header("调试")]
    [SerializeField] private float gizmoRadius = 0.22f;

    public GameObject CurrentInstance { get; private set; }
    public bool IsOccupied => CurrentInstance != null && CurrentInstance.activeInHierarchy;
    public Vector2 Position => transform.position;

    public void SetInstance(GameObject instance)
    {
        CurrentInstance = instance;
    }

    public void ClearInstance(GameObject instance)
    {
        if (CurrentInstance == instance) CurrentInstance = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}