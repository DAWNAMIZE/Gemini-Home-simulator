using UnityEngine;

public class Tap : MonoBehaviour, IInteractable
{
    public ParticleSystem waterStream;

    public void Interact()
    {
        if (waterStream != null)
        {
            if (waterStream.isPlaying)
                waterStream.Stop();
            else
                waterStream.Play();
        }
    }
}
