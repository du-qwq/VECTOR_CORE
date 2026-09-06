using UnityEngine;

[RequireComponent(typeof(CoreHealth))]
public class CoreDeathController : MonoBehaviour
{
    [Header("死亡后关闭")]
    [SerializeField] private Behaviour[] disableBehaviours;
    [SerializeField] private Collider2D[] disableColliders;
    [SerializeField] private GameObject visualRoot;

    [Header("消失")]
    [SerializeField] private bool hideVisualOnDeath = true;
    [SerializeField] private float visualHideDelay = 0.25f;

    private CoreHealth health;
    private Rigidbody2D rb;
    private bool eliminated;
    private float hideVisualTime;

    public bool IsEliminated => eliminated;

    private void Awake()
    {
        health = GetComponent<CoreHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        MatchManager.Instance?.RegisterCore(this);
    }

    private void Update()
    {
        if (!eliminated && health.IsDead) Eliminate();
        if (eliminated && hideVisualOnDeath && visualRoot != null && visualRoot.activeSelf && Time.time >= hideVisualTime) visualRoot.SetActive(false);
    }

    public void Eliminate()
    {
        if (eliminated) return;
        eliminated = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (disableBehaviours != null)
        {
            foreach (Behaviour behaviour in disableBehaviours)
            {
                if (behaviour != null) behaviour.enabled = false;
            }
        }

        if (disableColliders != null)
        {
            foreach (Collider2D col in disableColliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        hideVisualTime = Time.time + visualHideDelay;

        Debug.Log($"{name} ELIMINATED");
        MatchManager.Instance?.NotifyCoreEliminated(this);
    }
}