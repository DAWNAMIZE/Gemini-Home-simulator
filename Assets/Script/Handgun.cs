using UnityEngine;

public class Handgun : MonoBehaviour
{
    [Header("Shooting Setup")]
    [Tooltip("Create an Empty GameObject at the tip of the barrel and drag it here")]
    public Transform muzzlePoint; 
    
    [Tooltip("The bullet prefab to shoot")]
    public GameObject bulletPrefab;
    
    [Tooltip("How fast the bullet travels")]
    public float bulletForce = 50f;

    [Header("Slide Animation")]
    [Tooltip("Drag your 'Slide' child object here")]
    public Transform slideModel; 
    
    [Tooltip("How far back the slide kicks (Negative Z direction)")]
    public float slideKickbackDistance = 0.05f; 
    
    [Tooltip("How fast the slide snaps back into place")]
    public float slideSpeed = 20f;

    private Vector3 originalSlidePosition;
    private Vector3 targetSlidePosition;

    void Start()
    {
        // Record where the slide starts so it always returns exactly here
        if (slideModel != null)
        {
            originalSlidePosition = slideModel.localPosition;
            targetSlidePosition = originalSlidePosition;
        }
    }

    void Update()
    {
        // This smoothly moves the slide to its target position every frame
        if (slideModel != null)
        {
            slideModel.localPosition = Vector3.Lerp(slideModel.localPosition, targetSlidePosition, Time.deltaTime * slideSpeed);
            
            // Constantly pull the target position back to normal so the slide snaps forward after firing
            targetSlidePosition = Vector3.Lerp(targetSlidePosition, originalSlidePosition, Time.deltaTime * slideSpeed);
        }
    }

    // Call this function using a UI Fire button on your phone screen
    public void FireWeapon()
    {
        // 1. Spawn and shoot the projectile
        if (bulletPrefab != null && muzzlePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                // Shoot the bullet forward out of the barrel
                rb.AddForce(muzzlePoint.forward * bulletForce, ForceMode.Impulse);
            }
        }

        // 2. Trigger the Slide Recoil (Kicks backward along the Z axis)
        if (slideModel != null)
        {
            targetSlidePosition = originalSlidePosition - new Vector3(0, 0, slideKickbackDistance);
        }
    }
}