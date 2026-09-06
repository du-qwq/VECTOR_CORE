using UnityEngine;

public class ElementSpawnHandle : MonoBehaviour
{
    private ElementSpawnManager manager;
    private ElementSpawnPoint spawnPoint;
    private bool released;

    public void Initialize(ElementSpawnManager owner, ElementSpawnPoint point)
    {
        manager = owner;
        spawnPoint = point;
        released = false;
    }

    private void OnDestroy()
    {
        Release();
    }

    private void Release()
    {
        if (released) return;
        released = true;

        if (spawnPoint != null) spawnPoint.ClearInstance(gameObject);
        if (manager != null) manager.NotifyElementRemoved(gameObject);
    }
}