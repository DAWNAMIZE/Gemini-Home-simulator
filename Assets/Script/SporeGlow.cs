using UnityEngine;

/// <summary>
/// Controls the green glow light attached to a spore flier. Lives on the
/// SporeGlow prefab so its colour, size, brightness and pulse can be tuned in
/// the Inspector without touching code.
/// </summary>
[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class SporeGlow : MonoBehaviour
{
    [Header("Glow")]
    public Color glowColor = new Color(0.45f, 1f, 0.22f);

    [Tooltip("How far the glow reaches.")]
    [Min(0f)] public float lightRange = 3.5f;

    [Tooltip("Base brightness of the glow.")]
    [Min(0f)] public float lightIntensity = 1.2f;

    [Header("Pulse")]
    [Tooltip("Gently breathe the light while the flier is alive.")]
    public bool pulse = false;

    [Min(0f)] public float pulseSpeed = 2f;

    [Range(0f, 1f)]
    [Tooltip("How much the brightness changes while pulsing.")]
    public float pulseAmount = 0.15f;

    private Light glowLight;

    void Awake()
    {
        ApplySettings();
    }

    void OnValidate()
    {
        ApplySettings();
    }

    void Update()
    {
        if (glowLight == null) return;

        if (!pulse)
        {
            glowLight.intensity = lightIntensity;
            return;
        }

        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        glowLight.intensity = lightIntensity *
            Mathf.Lerp(1f - pulseAmount, 1f + pulseAmount, wave);
    }

    private void ApplySettings()
    {
        if (glowLight == null) glowLight = GetComponent<Light>();
        if (glowLight == null) return;

        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = lightRange;
        glowLight.intensity = lightIntensity;
        glowLight.shadows = LightShadows.None;
    }
}
