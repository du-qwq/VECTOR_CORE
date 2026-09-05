using UnityEngine;

public class CoreElementVisual : MonoBehaviour
{
    [SerializeField] private CoreElementStorage storage;
    [SerializeField] private SpriteRenderer slotAVisual;
    [SerializeField] private SpriteRenderer slotBVisual;

    [Header("颜色")]
    [SerializeField] private Color thermalColor = new Color(1f, 0.396f, 0.282f);
    [SerializeField] private Color fluidColor = new Color(0.196f, 0.824f, 0.706f);
    [SerializeField] private Color voltColor = new Color(1f, 0.831f, 0.278f);
    [SerializeField] private Color cryoColor = new Color(0.604f, 0.522f, 0.961f);

    private void Update()
    {
        UpdateSlot(slotAVisual, storage.SlotA);
        UpdateSlot(slotBVisual, storage.SlotB);
    }

    private void UpdateSlot(SpriteRenderer visual, ElementType element)
    {
        bool active = element != ElementType.None;
        visual.gameObject.SetActive(active);
        if (!active) return;
        visual.color = GetColor(element);
    }

    private Color GetColor(ElementType element)
    {
        return element switch
        {
            ElementType.Thermal => thermalColor,
            ElementType.Fluid => fluidColor,
            ElementType.Volt => voltColor,
            ElementType.Cryo => cryoColor,
            _ => Color.white
        };
    }
}