using System.Collections.Generic;
using UnityEngine;

public class ElementSpawnManager : MonoBehaviour
{
    [Header("元素 Prefab")]
    [SerializeField] private GameObject[] elementPrefabs;

    [Header("生成点")]
    [SerializeField] private Transform spawnPointRoot;

    [Header("生成数量")]
    [SerializeField] private int maxActiveElements = 5;
    [SerializeField] private int initialElements = 5;

    [Header("刷新")]
    [SerializeField] private float respawnDelay = 2.5f;
    [SerializeField] private float spawnInterval = 0.25f;

    [Header("空间检查")]
    [SerializeField] private float wallClearanceRadius = 0.45f;

    private readonly List<ElementSpawnPoint> spawnPoints = new List<ElementSpawnPoint>();
    private readonly List<GameObject> activeElements = new List<GameObject>();

    private float nextSpawnTime;
    private float respawnReadyTime;
    private int lastPrefabIndex = -1;

    public int ActiveElementCount => activeElements.Count;

    private void Awake()
    {
        CollectSpawnPoints();
    }

    private void Start()
    {
        int count = Mathf.Min(initialElements, maxActiveElements, spawnPoints.Count);
        for (int i = 0; i < count; i++) SpawnOne();
    }

    private void Update()
    {
        CleanupActiveElements();

        if (activeElements.Count >= maxActiveElements) return;
        if (Time.time < respawnReadyTime) return;
        if (Time.time < nextSpawnTime) return;

        if (SpawnOne()) nextSpawnTime = Time.time + spawnInterval;
    }

    private void CollectSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogWarning($"{name}: Spawn Point Root 没有设置。", this);
            return;
        }

        ElementSpawnPoint[] points = spawnPointRoot.GetComponentsInChildren<ElementSpawnPoint>(true);
        spawnPoints.AddRange(points);
    }

    private bool SpawnOne()
    {
        if (elementPrefabs == null || elementPrefabs.Length == 0) return false;
        if (spawnPoints.Count == 0) return false;

        List<ElementSpawnPoint> availablePoints = GetAvailableSpawnPoints();
        if (availablePoints.Count == 0) return false;

        ElementSpawnPoint point = availablePoints[Random.Range(0, availablePoints.Count)];
        GameObject prefab = GetRandomElementPrefab();

        if (prefab == null) return false;

        GameObject instance = Instantiate(prefab, point.transform.position, Quaternion.identity);
        instance.name = prefab.name;

        ElementSpawnHandle handle = instance.GetComponent<ElementSpawnHandle>();
        if (handle == null) handle = instance.AddComponent<ElementSpawnHandle>();

        handle.Initialize(this, point);
        point.SetInstance(instance);
        activeElements.Add(instance);

        return true;
    }

    private List<ElementSpawnPoint> GetAvailableSpawnPoints()
    {
        List<ElementSpawnPoint> result = new List<ElementSpawnPoint>();

        foreach (ElementSpawnPoint point in spawnPoints)
        {
            if (point == null || point.IsOccupied) continue;
            if (IsBlockedByWall(point.Position)) continue;
            result.Add(point);
        }

        return result;
    }

    private bool IsBlockedByWall(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, wallClearanceRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag("Wall")) return true;
        }

        return false;
    }

    private GameObject GetRandomElementPrefab()
    {
        if (elementPrefabs.Length == 1) return elementPrefabs[0];

        int index = Random.Range(0, elementPrefabs.Length);

        for (int i = 0; i < 6 && index == lastPrefabIndex; i++) index = Random.Range(0, elementPrefabs.Length);

        lastPrefabIndex = index;
        return elementPrefabs[index];
    }

    private void CleanupActiveElements()
    {
        for (int i = activeElements.Count - 1; i >= 0; i--)
        {
            GameObject element = activeElements[i];
            if (element == null || !element.activeInHierarchy) activeElements.RemoveAt(i);
        }
    }

    public void NotifyElementRemoved(GameObject element)
    {
        activeElements.Remove(element);
        respawnReadyTime = Mathf.Max(respawnReadyTime, Time.time + respawnDelay);
    }
}