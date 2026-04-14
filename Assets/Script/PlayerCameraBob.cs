using UnityEngine;

public class PlayerCameraBob : MonoBehaviour
{
    [Header("Bobbing Settings")]
    public float bobSpeed = 12f;
    public float bobAmount = 0.05f;
    public Transform playerBody; // The root object that actually moves

    [Header("Footstep Audio")]
    public AudioSource footstepSource;
    public AudioClip[] footstepSounds; // Array so we can add multiple sounds!

    private float defaultY;
    private float timer = 0;
    private bool hasStepped = false;
    private Vector3 lastPlayerPos;

    void Start()
    {
        // Store the camera's starting height
        defaultY = transform.localPosition.y;
        
        if (playerBody != null) 
        {
            lastPlayerPos = playerBody.position;
        }
    }

    void Update()
    {
        if (playerBody == null) return;

        // Calculate actual movement speed (Works with ANY joystick or movement code!)
        float speed = Vector3.Distance(playerBody.position, lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = playerBody.position;

        // Are we moving fast enough to trigger a step?
        if (speed > 0.1f)
        {
            // Move the sine wave forward based on speed
            timer += Time.deltaTime * bobSpeed;
            
            // Calculate the new Y position of the camera
            float newY = defaultY + Mathf.Sin(timer) * bobAmount;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

            // Sync the audio: Play a footstep exactly when the camera bobs to the lowest point!
            if (Mathf.Sin(timer) < -0.9f && !hasStepped)
            {
                PlayFootstep();
                hasStepped = true;
            }
            else if (Mathf.Sin(timer) > 0f) // Reset the trigger when the head goes back up
            {
                hasStepped = false;
            }
        }
        else
        {
            // Smoothly return the camera to the center when standing still
            timer = 0;
            float resetY = Mathf.Lerp(transform.localPosition.y, defaultY, Time.deltaTime * 5f);
            transform.localPosition = new Vector3(transform.localPosition.x, resetY, transform.localPosition.z);
        }
    }

    void PlayFootstep()
    {
        // Make sure we actually have sounds loaded
        if (footstepSounds.Length > 0 && footstepSource != null)
        {
            // Pick a random footstep sound from the list so it doesn't sound repetitive
            int randomIndex = Random.Range(0, footstepSounds.Length);
            footstepSource.clip = footstepSounds[randomIndex];
            
            // Randomize the pitch slightly to make the steps sound natural and human
            footstepSource.pitch = Random.Range(0.85f, 1.15f);
            footstepSource.Play();
        }
    }
}