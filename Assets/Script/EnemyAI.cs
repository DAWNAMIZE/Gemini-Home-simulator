using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 100;
    
    [Header("Detection Range")]
    public float chaseRange = 15f;
    public float attackRange = 7f;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float wanderTimer = 4f;
    private float timeSinceLastWander;

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
        timeSinceLastWander = wanderTimer; 
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetPlayer = playerObj.transform;
    }

    void Update()
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange)
        {
            // State 1: Attack
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
            // State 2: Chase
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);
        }
        else
        {
            // State 3: Wander randomly
            agent.isStopped = false;
            timeSinceLastWander += Time.deltaTime;

            if (timeSinceLastWander >= wanderTimer)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                agent.SetDestination(newPos);
                timeSinceLastWander = 0;
            }
        }
    }

    private void AimAtPlayer()
    {
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
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
        if (health <= 0) Destroy(gameObject);
    }

    private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}