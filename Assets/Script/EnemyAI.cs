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

    [Header("Door Entry")]
    [Tooltip("Damage dealt to a closed door each hit.")]
    public float doorAttackDamage = 15f;
    [Tooltip("Seconds between hits on a closed door.")]
    public float doorAttackInterval = 0.75f;
    [Tooltip("How close the enemy must get to a door before attacking it.")]
    public float doorAttackRange = 3f;
    [Tooltip("Safety timeout for walking through an open door.")]
    public float doorPassTimeout = 8f;

    private float nextFireTime;
    private float nextDoorAttackTime;
    private Transform targetPlayer;
    private NavMeshAgent agent;

    private Door entryDoor;
    private bool doorEntryActive;
    private bool passingDoor;
    private int doorHoldId = -1;
    private float doorEntrySide;
    private float doorPassStartTime;
    private Vector3 doorExitPoint;

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

        // The enemy may only cross between inside and outside through a door.
        // If a door separates it from the player, that door becomes the goal.
        Door separatingDoor = Door.FindDoorBetween(transform.position, targetPlayer.position);

        bool shouldUseDoor = separatingDoor != null &&
            (doorEntryActive ||
             distanceToPlayer <= chaseRange ||
             separatingDoor.HorizontalDistanceTo(transform.position) <= chaseRange);

        if (shouldUseDoor)
        {
            doorEntryActive = true;
            UpdateDoorEntry(separatingDoor);
            return;
        }

        if (doorEntryActive)
        {
            doorEntryActive = false;
            ReleaseDoor();
        }

        UpdateCombat(distanceToPlayer);
    }

    // ------------------------------------------------------------------
    // DOOR ENTRY
    // ------------------------------------------------------------------

    private void UpdateDoorEntry(Door door)
    {
        entryDoor = door;

        if (agent == null) return;

        if (passingDoor)
        {
            ContinuePassingDoor(door);
            return;
        }

        // The player may have opened (or broken) the door already.
        if (door.IsPassable)
        {
            BeginPassingDoor(door);
            return;
        }

        bool inAttackRange = door.HorizontalDistanceTo(transform.position) <= doorAttackRange;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = inAttackRange;

            if (!inAttackRange)
                agent.SetDestination(door.GetApproachPoint(transform.position));
        }

        if (!inAttackRange) return;

        FacePoint(door.Center);

        if (Time.time >= nextDoorAttackTime)
        {
            door.TakeDamage(doorAttackDamage);
            nextDoorAttackTime = Time.time + Mathf.Max(0.05f, doorAttackInterval);
        }
    }

    private void BeginPassingDoor(Door door)
    {
        passingDoor = true;
        doorPassStartTime = Time.time;

        doorEntrySide = Mathf.Sign(door.SignedDistance(transform.position));
        if (doorEntrySide == 0f) doorEntrySide = 1f;

        doorExitPoint = door.GetExitPoint(transform.position);

        // Keep the door from swinging shut while the enemy is in the doorway.
        if (doorHoldId < 0)
            doorHoldId = door.AcquireOpenHold();

        door.EnsureNavLink(agent != null ? agent.agentTypeID : 0);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(doorExitPoint);
        }
    }

    private void ContinuePassingDoor(Door door)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (!agent.hasPath || (agent.destination - doorExitPoint).sqrMagnitude > 0.25f)
                agent.SetDestination(doorExitPoint);
        }

        if (Time.time - doorPassStartTime <= doorPassTimeout) return;

        float sideDistance = door.SignedDistance(transform.position);
        bool crossed = sideDistance * doorEntrySide < 0f;

        // Last-resort fallback if the NavMesh link could not be traversed.
        if (!crossed && agent != null && agent.isOnNavMesh &&
            door.HorizontalDistanceTo(transform.position) <= doorAttackRange + 0.5f)
        {
            agent.Warp(doorExitPoint);
        }

        ReleaseDoor();
    }

    private void ReleaseDoor()
    {
        if (doorHoldId >= 0)
        {
            if (entryDoor != null)
                entryDoor.ReleaseOpenHold(doorHoldId);

            doorHoldId = -1;
        }

        entryDoor = null;
        passingDoor = false;
    }

    void OnDisable()
    {
        ReleaseDoor();
        doorEntryActive = false;
    }

    // ------------------------------------------------------------------
    // NORMAL COMBAT
    // ------------------------------------------------------------------

    private void UpdateCombat(float distanceToPlayer)
    {
        if (agent == null) return;

        if (distanceToPlayer <= attackRange)
        {
            // State 1: Attack
            if (agent.isOnNavMesh) agent.isStopped = true;
            AimAtPlayer();

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // State 2: Chase
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetPlayer.position);
            }
        }
        else
        {
            // State 3: Wander randomly
            if (agent.isOnNavMesh) agent.isStopped = false;
            timeSinceLastWander += Time.deltaTime;

            if (timeSinceLastWander >= wanderTimer)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);

                if (agent.isOnNavMesh)
                    agent.SetDestination(newPos);

                timeSinceLastWander = 0;
            }
        }
    }

    private void AimAtPlayer()
    {
        FacePoint(targetPlayer.position);
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 direction = (point - transform.position);
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
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

        if (NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, layermask))
            return navHit.position;

        return origin;
    }
}
