using System.Collections;
using UnityEngine;

/// <summary>
/// Bathroom shower. Interacting cleans the player and plays the shower effects.
/// </summary>
public class BathroomShower : MonoBehaviour, IInteractable
{
    [Header("Bathing")]
    [Tooltip("Hygiene restored by one shower (0-100 scale).")]
    public float hygieneAmount = 30f;
    public float useCooldown = 1f;

    [Header("Shower Effects")]
    public AudioSource showerAudio;
    public AudioClip showerSound;
    [Range(0f, 1f)] public float showerVolume = 0.8f;
    public ParticleSystem waterSpray;
    public float sprayDuration = 2.5f;

    private float nextUseTime;
    private Coroutine sprayRoutine;

    public void Interact()
    {
        if (Time.unscaledTime < nextUseTime) return;

        PlayerVitals vitals = PlayerVitals.instance;
        if (vitals == null)
        {
            ToastUI.Show("You cannot shower right now.");
            return;
        }

        nextUseTime = Time.unscaledTime + Mathf.Max(0f, useCooldown);

        float before = vitals.hygiene;
        vitals.Clean(hygieneAmount);
        float restored = Mathf.Max(0f, vitals.hygiene - before);

        PlayShowerEffects();

        if (restored < 0.1f)
            ToastUI.Show("You are already clean.");
        else
            ToastUI.Show("You took a shower. Hygiene +" + Mathf.RoundToInt(restored) + ".");
    }

    private void PlayShowerEffects()
    {
        if (showerSound != null)
        {
            if (showerAudio == null) showerAudio = GetComponent<AudioSource>();
            if (showerAudio == null) showerAudio = gameObject.AddComponent<AudioSource>();

            showerAudio.playOnAwake = false;
            showerAudio.spatialBlend = 1f;
            showerAudio.PlayOneShot(showerSound, showerVolume);
        }

        if (waterSpray == null) return;

        waterSpray.Play();
        if (sprayRoutine != null) StopCoroutine(sprayRoutine);
        sprayRoutine = StartCoroutine(StopSprayAfterDelay());
    }

    private IEnumerator StopSprayAfterDelay()
    {
        yield return new WaitForSeconds(sprayDuration);
        if (waterSpray != null) waterSpray.Stop();
        sprayRoutine = null;
    }
}
