using UnityEngine;

/// <summary>
/// Drinkable water well. Interacting restores thirst and plays the assigned
/// drinking-water sound.
/// </summary>
public class WaterWell : MonoBehaviour, IInteractable
{
    [Header("Drinking")]
    [Tooltip("Thirst restored by one drink (0-100 scale).")]
    public float drinkAmount = 30f;
    public float useCooldown = 0.5f;

    [Header("Drink Sound")]
    public AudioSource drinkAudio;
    public AudioClip drinkSound;
    [Range(0f, 1f)] public float drinkVolume = 0.85f;

    private float nextUseTime;

    public void Interact()
    {
        if (Time.unscaledTime < nextUseTime) return;

        PlayerVitals vitals = PlayerVitals.instance;
        if (vitals == null)
        {
            ToastUI.Show("You cannot drink right now.");
            return;
        }

        nextUseTime = Time.unscaledTime + Mathf.Max(0f, useCooldown);

        float before = vitals.thirst;
        vitals.Drink(drinkAmount);
        float restored = Mathf.Max(0f, vitals.thirst - before);

        PlayDrinkSound();

        if (restored < 0.1f)
            ToastUI.Show("You are not thirsty right now.");
        else
            ToastUI.Show("Fresh well water. Thirst +" + Mathf.RoundToInt(restored) + ".");
    }

    private void PlayDrinkSound()
    {
        if (drinkSound == null) return;

        if (drinkAudio == null) drinkAudio = GetComponent<AudioSource>();
        if (drinkAudio == null) drinkAudio = gameObject.AddComponent<AudioSource>();

        drinkAudio.playOnAwake = false;
        drinkAudio.spatialBlend = 1f;
        drinkAudio.PlayOneShot(drinkSound, drinkVolume);
    }
}
