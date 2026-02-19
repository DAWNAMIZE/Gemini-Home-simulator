using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class DoorTransition : MonoBehaviour, IInteractable
{
    [Header("Destination")]
    [Tooltip("Type the EXACT name of your outdoor scene here.")]
    public string sceneToLoad = "OutdoorScene"; // Change this to your scene's name!

    [Header("Audio (Optional)")]
    public AudioSource doorAudioSource;
    public AudioClip doorOpenSound;

    // This gets called when the player looks at the door and clicks
    public void Interact()
    {
        // Optional: Play a sound right before loading (though it might get cut off if it loads too fast)
        if (doorAudioSource != null && doorOpenSound != null)
        {
            doorAudioSource.PlayOneShot(doorOpenSound);
        }

        Debug.Log("Loading Scene: " + sceneToLoad);
        
        // Load the new scene
        SceneManager.LoadScene(sceneToLoad);
    }
}