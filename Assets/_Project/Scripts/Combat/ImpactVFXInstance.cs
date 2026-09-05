using UnityEngine;

public class ImpactVFXInstance : MonoBehaviour
{
    [Header("Core冲击环")]
    [SerializeField] private MeshRenderer ringRenderer;
    [SerializeField] private float coreRingLifetime = 0.16f;
    [SerializeField] private float minRingScale = 0.75f;
    [SerializeField] private float maxRingScale = 1.25f;

    [Header("墙体闪光")]
    [SerializeField] private MeshRenderer wallFlashRenderer;
    [SerializeField] private float wallFlashLifetime = 0.10f;
    [SerializeField] private float minWallFlashScale = 0.75f;
    [SerializeField] private float maxWallFlashScale = 1.30f;

    [Header("Sparks")]
    [SerializeField] private ParticleSystem sparks;

    [Header("Core火花")]
    [SerializeField] private int coreMinSparkCount = 7;
    [SerializeField] private int coreMaxSparkCount = 15;
    [SerializeField] private float coreSparkSpreadAngle = 150f;

    [Header("墙体火花")]
    [SerializeField] private int wallMinSparkCount = 4;
    [SerializeField] private int wallMaxSparkCount = 10;
    [SerializeField] private float wallSparkTangentSpread = 22f;

    [Header("火花通用")]
    [SerializeField] private float minSparkSpeed = 2.5f;
    [SerializeField] private float maxSparkSpeed = 6.5f;

    [SerializeField] private float minSparkLifetime = 0.07f;
    [SerializeField] private float maxSparkLifetime = 0.16f;

    [SerializeField] private float minSparkSize = 0.025f;
    [SerializeField] private float maxSparkSize = 0.065f;

    [Header("颜色")]
    [SerializeField] private Color wallColor = new Color(0.22f, 0.68f, 0.90f, 1f);
    [SerializeField] private Color coreColor = new Color(0.72f, 0.96f, 1.00f, 1f);

    private static readonly int ProgressID = Shader.PropertyToID("_Progress");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");

    private MaterialPropertyBlock ringBlock;
    private MaterialPropertyBlock wallBlock;

    private float timer;
    private float lifetime;
    private float intensity;

    private bool playing;
    private bool coreImpact;

    private void Awake()
    {
        ringBlock = new MaterialPropertyBlock();
        wallBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!playing) return;

        timer += Time.deltaTime;

        float progress = lifetime <= 0f ? 1f : Mathf.Clamp01(timer / lifetime);

        if (coreImpact) UpdateCoreRing(progress);
        else UpdateWallFlash(progress);

        if (progress < 1f) return;

        playing = false;

        float particleLife = sparks != null ? maxSparkLifetime : 0f;
        Destroy(gameObject, particleLife + 0.05f);
    }

    public void Play(Vector2 position, Vector2 normal, float impact01, bool hitCore)
    {
        transform.position = new Vector3(position.x, position.y, 0.05f);

        intensity = Mathf.Clamp01(impact01);
        coreImpact = hitCore;

        if (ringRenderer != null) ringRenderer.gameObject.SetActive(hitCore);
        if (wallFlashRenderer != null) wallFlashRenderer.gameObject.SetActive(!hitCore);

        if (hitCore)
        {
            lifetime = coreRingLifetime;
            SetupCoreRing();
            EmitCoreSparks(normal);
        }
        else
        {
            lifetime = wallFlashLifetime;
            SetupWallFlash(normal);
            EmitWallSparks(normal);
        }

        timer = 0f;
        playing = true;
    }

    private void SetupCoreRing()
    {
        if (ringRenderer == null) return;

        float scale = Mathf.Lerp(minRingScale, maxRingScale, intensity);

        ringRenderer.transform.localScale = new Vector3(scale, scale, 1f);

        ringRenderer.GetPropertyBlock(ringBlock);

        ringBlock.SetFloat(ProgressID, 0f);
        ringBlock.SetColor(ColorID, coreColor);
        ringBlock.SetFloat(IntensityID, Mathf.Lerp(1.1f, 2.2f, intensity));

        ringRenderer.SetPropertyBlock(ringBlock);
    }

    private void UpdateCoreRing(float progress)
    {
        if (ringRenderer == null) return;

        ringRenderer.GetPropertyBlock(ringBlock);
        ringBlock.SetFloat(ProgressID, progress);
        ringRenderer.SetPropertyBlock(ringBlock);
    }

    private void SetupWallFlash(Vector2 normal)
    {
        if (wallFlashRenderer == null) return;

        if (normal.sqrMagnitude < 0.001f) normal = Vector2.up;
        normal.Normalize();

        Vector2 tangent = new Vector2(-normal.y, normal.x);

        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

        wallFlashRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        float scale = Mathf.Lerp(minWallFlashScale, maxWallFlashScale, intensity);

        wallFlashRenderer.transform.localScale = new Vector3(scale, scale, 1f);

        wallFlashRenderer.GetPropertyBlock(wallBlock);

        wallBlock.SetFloat(ProgressID, 0f);
        wallBlock.SetColor(ColorID, wallColor);
        wallBlock.SetFloat(IntensityID, Mathf.Lerp(1.2f, 2.4f, intensity));

        wallFlashRenderer.SetPropertyBlock(wallBlock);
    }

    private void UpdateWallFlash(float progress)
    {
        if (wallFlashRenderer == null) return;

        wallFlashRenderer.GetPropertyBlock(wallBlock);
        wallBlock.SetFloat(ProgressID, progress);
        wallFlashRenderer.SetPropertyBlock(wallBlock);
    }

    private void EmitCoreSparks(Vector2 normal)
    {
        if (sparks == null) return;

        if (normal.sqrMagnitude < 0.001f) normal = Vector2.up;
        normal.Normalize();

        int count = Mathf.RoundToInt(Mathf.Lerp(coreMinSparkCount, coreMaxSparkCount, intensity));

        for (int i = 0; i < count; i++)
        {
            Vector2 direction;

            if (Random.value < 0.65f)
            {
                float angle = Random.Range(-coreSparkSpreadAngle * 0.5f, coreSparkSpreadAngle * 0.5f);
                direction = Rotate(normal, angle);
            }
            else
            {
                float angle = Random.Range(0f, 360f);
                direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            }

            EmitParticle(direction, coreColor);
        }
    }

    private void EmitWallSparks(Vector2 normal)
    {
        if (sparks == null) return;

        if (normal.sqrMagnitude < 0.001f) normal = Vector2.up;
        normal.Normalize();

        Vector2 tangent = new Vector2(-normal.y, normal.x);

        int count = Mathf.RoundToInt(Mathf.Lerp(wallMinSparkCount, wallMaxSparkCount, intensity));

        for (int i = 0; i < count; i++)
        {
            Vector2 side = Random.value < 0.5f ? tangent : -tangent;

            float angle = Random.Range(-wallSparkTangentSpread, wallSparkTangentSpread);

            Vector2 direction = Rotate(side, angle);

            // 少量往外弹，避免火花完全贴墙
            direction = (direction + normal * Random.Range(0.05f, 0.30f)).normalized;

            EmitParticle(direction, wallColor);
        }
    }

    private void EmitParticle(Vector2 direction, Color color)
    {
        float speed = Random.Range(minSparkSpeed, maxSparkSpeed);
        speed *= Mathf.Lerp(0.65f, 1.30f, intensity);

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = Vector3.zero,
            velocity = new Vector3(direction.x, direction.y, 0f) * speed,
            startLifetime = Random.Range(minSparkLifetime, maxSparkLifetime),
            startSize = Random.Range(minSparkSize, maxSparkSize),
            startColor = color
        };

        sparks.Emit(emitParams, 1);
    }

    private Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
}