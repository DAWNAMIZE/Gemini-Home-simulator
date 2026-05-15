using UnityEngine;

public class PickupRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("How fast the item spins. Y is usually best for items!")]
    public Vector3 rotationSpeed = new Vector3(0f, 50f, 0f);

    [Header("Bobbing Settings (Floating)")]
    public bool enableBobbing = true;
    public float bobSpeed = 2f;      // How fast it moves up and down
    public float bobHeight = 0.1f;   // How high it goes

    private Vector3 startPosition;

    void Start()
    {
        // Remember where the item started so it doesn't float away into the sky!
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. Spin the item
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // 2. Make the item float up and down smoothly using a Sine wave
        if (enableBobbing)
        {
            float newY = startPosition.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}
