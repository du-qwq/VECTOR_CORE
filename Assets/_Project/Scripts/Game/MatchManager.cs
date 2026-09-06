using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public enum MatchState
    {
        Waiting,
        Playing,
        Finished
    }

    public static MatchManager Instance { get; private set; }

    [Header("比赛")]
    [SerializeField] private bool autoStart = true;

    private readonly List<CoreDeathController> cores = new List<CoreDeathController>();
    private MatchState state = MatchState.Waiting;

    public MatchState State => state;
    public int TotalCoreCount => cores.Count;
    public int AliveCoreCount
    {
        get
        {
            int count = 0;
            foreach (CoreDeathController core in cores)
            {
                if (core != null && !core.IsEliminated) count++;
            }
            return count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (autoStart) StartMatch();
    }

    public void RegisterCore(CoreDeathController core)
    {
        if (core == null || cores.Contains(core)) return;
        cores.Add(core);
    }

    public void StartMatch()
    {
        state = MatchState.Playing;
        Debug.Log($"MATCH START - {AliveCoreCount} CORES");
    }

    public void NotifyCoreEliminated(CoreDeathController eliminatedCore)
    {
        if (state != MatchState.Playing) return;

        int alive = AliveCoreCount;
        Debug.Log($"ALIVE CORES：{alive}");

        if (alive <= 1) FinishMatch();
    }

    private void FinishMatch()
    {
        if (state == MatchState.Finished) return;
        state = MatchState.Finished;

        CoreDeathController winner = GetLastAliveCore();

        if (winner != null) Debug.Log($"WINNER：{winner.name}");
        else Debug.Log("MATCH END：NO WINNER");
    }

    private CoreDeathController GetLastAliveCore()
    {
        foreach (CoreDeathController core in cores)
        {
            if (core != null && !core.IsEliminated) return core;
        }

        return null;
    }
}