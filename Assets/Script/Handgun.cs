using UnityEngine;
using System.Collections;

public class Handgun : MonoBehaviour
{
    [Header("Shooting Setup")]
    [Tooltip("Empty GameObject at the tip of the barrel")]
    public Transform muzzlePoint;

    [Tooltip("The bullet prefab to shoot")]
    public GameObject bulletPrefab;

    [Tooltip("How fast the bullet travels")]
    public float bulletForce = 50f;

    [Header("Muzzle Flash")]
    [Tooltip("Particle system / light object at the barrel tip (inactive by default)")]
    public GameObject muzzleFlash;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    [Range(0f, 1f)] public float fireVolume = 0.8f;

    [Header("Bullet Tracer")]
    [Tooltip("Optional line or trail prefab that follows the bullet for a short time")]
    public GameObject tracerPrefab;
    [Tooltip("How long the tracer stays visible")]
    public float tracerLifetime = 0.15f;

    [Header("Slide Animation")]
    public Transform slideModel;
    public float slideKickbackDistance = 0.05f;
    public float slideSpeed = 20f;

    private Vector3 originalSlidePosition;
    private Vector3 targetSlidePosition;

    void Start()
    {
        if (slideModel != null)
        {
            originalSlidePosition = slideModel.localPosition;
            targetSlidePosition = originalSlidePosition;
        }

        // Create an AudioSource if one wasn't assigned
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
    }

    void Update()
    {
        if (slideModel != null)
        {
            slideModel.localPosition = Vector3.Lerp(slideModel.localPosition, targetSlidePosition, Time.deltaTime * slideSpeed);
            targetSlidePosition = Vector3.Lerp(targetSlidePosition, originalSlidePosition, Time.deltaTime * slideSpeed);
        }
    }

    public void FireWeapon()
    {
        // 1. Spawn bullet
        if (bulletPrefab != null && muzzlePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
                rb.AddForce(muzzlePoint.forward * bulletForce, ForceMode.Impulse);

            // 2. Bullet tracer
            if (tracerPrefab != null)
            {
                GameObject tracer = Instantiate(tracerPrefab, muzzlePoint.position, muzzlePoint.rotation);
                // Parent the tracer to the bullet so it follows it
                tracer.transform.SetParent(bullet.transform, true);
                Destroy(tracer, tracerLifetime);
            }
        }

        // 3. Muzzle flash
        if (muzzleFlash != null)
            StartCoroutine(PlayMuzzleFlash());

        // 4. Fire sound
        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound, fireVolume);

        // 5. Slide recoil
        if (slideModel != null)
            targetSlidePosition = originalSlidePosition - new Vector3(0, 0, slideKickbackDistance);
    }

    private IEnumerator PlayMuzzleFlash()
    {
        muzzleFlash.SetActive(true);

        ParticleSystem ps = muzzleFlash.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        Light flashLight = muzzleFlash.GetComponentInChildren<Light>();
        if (flashLight != null) flashLight.enabled = true;

        yield return new WaitForSeconds(0.05f);

        if (ps != null) ps.Stop();
        if (flashLight != null) flashLight.enabled = false;

        muzzleFlash.SetActive(false);
    }
}