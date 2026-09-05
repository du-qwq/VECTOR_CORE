using UnityEngine;

public class CoreImpactVFXEmitter : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private ImpactVFXInstance impactPrefab;

    [Header("触发")]
    [SerializeField] private float minImpactSpeed = 2.5f;
    [SerializeField] private float maxImpactSpeed = 15f;

    [Header("类型")]
    [SerializeField] private bool spawnOnWall = true;
    [SerializeField] private bool spawnOnCore = true;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (impactPrefab == null || collision.contactCount == 0) return;

        bool hitWall = collision.collider.CompareTag("Wall");
        bool hitCore = collision.collider.GetComponentInParent<CoreCombat>() != null;

        if (hitWall && !spawnOnWall) return;
        if (hitCore && !spawnOnCore) return;
        if (!hitWall && !hitCore) return;

        ContactPoint2D contact = collision.GetContact(0);

        float impactSpeed = Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal));
        if (impactSpeed < minImpactSpeed) return;

        float impact01 = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, impactSpeed);

        ImpactVFXInstance instance = Instantiate(impactPrefab);

        instance.Play(
            contact.point,
            contact.normal,
            impact01,
            hitCore
        );
    }
}