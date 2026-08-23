using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("How many seconds before it explodes in the air")]
    public float explosionTimer = 3f;

    [Header("Effects")]
    [Tooltip("Drag a particle system prefab here (Optional)")]
    public GameObject explosionVFX;

    void Start()
    {
        // This tells Unity to run the "Explode" function exactly 3 seconds from now
        Invoke(nameof(Explode), explosionTimer);
    }

    // This triggers if the bullet hits a wall or the player before the 3 seconds are up
    void OnCollisionEnter(Collision collision)
    {
        // If the bullet hits the enemy that shot it, ignore it and keep flying!
        if (collision.gameObject.CompareTag("Enemy"))
        {
            return; 
        }

        // Cancel the 3-second timer since it hit a wall or the player
        CancelInvoke(nameof(Explode)); 
        
        Explode();
    }

    private void Explode()
    {
        // 1. Spawn a visual explosion effect if you assigned one
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, transform.rotation);
        }

        // 2. Destroy the bullet itself
        Destroy(gameObject);
    }
}