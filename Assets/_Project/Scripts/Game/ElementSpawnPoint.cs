using UnityEngine;

public class ElementSpawnPoint : MonoBehaviour
{
    [SerializeField] private float gizmoRadius = 0.22f;

    public GameObject CurrentInstance { get; private set; }
    public bool IsOccupied => CurrentInstance != null;
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