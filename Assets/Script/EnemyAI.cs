using UnityEngine;
using UnityEngine.AI; // Required to use NavMesh

[RequireComponent(typeof(NavMeshAgent))] // Automatically adds the Agent to your sphere
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 100;
    
    [Header("Detection Range")]
    public float chaseRange = 15f;
    public float attackRange = 7f;

    [Header("Combat Settings")]
    public GameObject enemyProjectilePrefab;
    public Transform firePoint;
    public float projectileForce = 15f;
    public float fireRate = 1.5f; 
    
    private float nextFireTime;
    private Transform targetPlayer;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
        }
    }

    void Update()
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange)
        {
            // Stop the agent's movement and shoot
            agent.isStopped = true; 
            AimAtPlayer();
            
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + (1f / fireRate);
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // Resume movement and plot a path to the player
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);
        }
        else
        {
            // Player is out of range, stand still
            agent.isStopped = true;
        }
    }

    private void AimAtPlayer()
    {
        // Smoothly rotate to face the player without tilting up or down
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0; 
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void Shoot()
    {
        if (enemyProjectilePrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(enemyProjectilePrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(firePoint.forward * projectileForce, ForceMode.Impulse);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log("Enemy took damage! Remaining Health: " + health);
        if (health <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("Enemy Defeated!");
        Destroy(gameObject);
    }
}