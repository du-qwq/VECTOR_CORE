using System.Collections.Generic;
using UnityEngine;

public class ElementSpawnManager : MonoBehaviour
{
    [Header("元素")]
    [SerializeField] private GameObject[] elementPrefabs;

    [Header("生成点")]
    [SerializeField] private Transform spawnPointRoot;

    [Header("数量")]
    [SerializeField] private int initialElementCount = 5;
    [SerializeField] private int maxActiveElements = 5;

    [Header("刷新")]
    [SerializeField] private float respawnDelay = 2.5f;
    [SerializeField] private float spawnInterval = 0.25f;

    [Header("空间检查")]
    [SerializeField] private float wallClearanceRadius = 0.45f;
    [SerializeField] private LayerMask blockingLayers = ~0;

    private readonly List<ElementSpawnPoint> spawnPoints = new List<ElementSpawnPoint>();
    private readonly List<GameObject> activeElements = new List<GameObject>();

    private float nextSpawnTime;
    private float respawnUnlockTime;
    private int lastPrefabIndex = -1;

    public int ActiveElementCount => activeElements.Count;

    private void Awake()
    {
        CollectSpawnPoints();
    }

    private void Start()
    {
        int spawnCount = Mathf.Min(initialElementCount, maxActiveElements, spawnPoints.Count);
        for (int i = 0; i < spawnCount; i++) SpawnOne();
    }

    private void Update()
    {
        CleanupActiveElements();

        if (activeElements.Count >= maxActiveElements) return;
        if (Time.time < respawnUnlockTime) return;
        if (Time.time < nextSpawnTime) return;

        if (SpawnOne()) nextSpawnTime = Time.time + spawnInterval;
    }

    private void CollectSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogWarning($"{name}：没有设置 Spawn Point Root。", this);
            return;
        }

        spawnPoints.AddRange(spawnPointRoot.GetComponentsInChildren<ElementSpawnPoint>(true));
    }

    private bool SpawnOne()
    {
        if (elementPrefabs == null || elementPrefabs.Length == 0) return false;

        List<ElementSpawnPoint> availablePoints = GetAvailableSpawnPoints();
        if (availablePoints.Count == 0) return false;

        ElementSpawnPoint point = availablePoints[Random.Range(0, availablePoints.Count)];
        GameObject prefab = GetRandomPrefab();
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
            if (IsBlocked(point.Position)) continue;
            result.Add(point);
        }

        return result;
    }

    private bool IsBlocked(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, wallClearanceRadius, blockingLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag("Wall")) return true;
        }

        return false;
    }

    private GameObject GetRandomPrefab()
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
            if (activeElements[i] == null) activeElements.RemoveAt(i);
        }
    }

    public void NotifyElementRemoved(GameObject element)
    {
        activeElements.Remove(element);
        respawnUnlockTime = Mathf.Max(respawnUnlockTime, Time.time + respawnDelay);
    }
}