using System;
using UnityEngine;
using UnityEngine.AI;

public enum CustomerState
{
    Entering,
    Seating,
    WaitingForFood,
    Eating,
    Leaving,
    Gone
}

/// <summary>
/// A single restaurant customer. The manager spawns it at the spawn point, it
/// walks to Customer-Seat-01, shows its order, waits for the correct dish,
/// eats, pays and then walks back out of the restaurant.
/// </summary>
public class CustomerAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.2f;
    public float leaveSpeed = 4.2f;
    public float turnSpeed = 8f;
    public float groundProbeHeight = 4f;
    public float groundProbeDistance = 14f;
    public float maxStepUp = 0.85f;
    public float verticalFollowSpeed = 6f;

    [Header("Order")]
    public string requestedDish = "";
    public float eatDuration = 9f;
    public int payment = 25;

    [Header("References")]
    public Transform seat;
    public Transform exitPoint;
    public CustomerTray tray;

    [Header("NavMesh")]
    [Tooltip("How far off the direct walking line the agent may look for a reachable NavMesh hand-off point.")]
    public float navMeshSearchDistance = 4f;
    public float navArriveDistance = 0.45f;

    public CustomerState State { get; private set; } = CustomerState.Entering;
    public bool IsEating => State == CustomerState.Eating;

    public event Action<CustomerAI> Finished;

    private CustomerOrderBubble bubble;
    private CapsuleCollider bodyCollider;
    private float capsuleHalfHeight = 1f;
    private Vector3 seatedPosition;
    private Quaternion seatedRotation;
    private float groundY;
    private bool hasGroundY;
    private Vector3 seatingStart;
    private float seatingTimer;
    private float eatTimer;
    private FoodDish eatingDish;
    private float leaveTimer;
    private float enteringTimer;
    private bool initialized;
    private bool finished;

    private NavMeshAgent agent;
    private bool navReady;
    private bool usingAgent;
    private Vector3 navSeatPoint;
    private Vector3 navEntryPoint;

    void Awake()
    {
        bodyCollider = GetComponent<CapsuleCollider>();
        if (bodyCollider != null)
            capsuleHalfHeight = Mathf.Max(0.2f, bodyCollider.bounds.extents.y);

        if (capsuleHalfHeight <= 0.21f)
            capsuleHalfHeight = 1f;

        EnsureAgent();
    }

    void OnDestroy()
    {
        if (tray != null)
            tray.ClearCustomer(this);

        if (!finished)
        {
            finished = true;
            Finished?.Invoke(this);
        }
    }

    public void Initialize(Transform seatPoint, Transform exit, CustomerTray customerTray,
        string dishName, Vector3 spawnPosition)
    {
        seat = seatPoint;
        exitPoint = exit;
        tray = customerTray;
        requestedDish = dishName;

        if (bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider>();
        if (bodyCollider != null)
            capsuleHalfHeight = Mathf.Max(0.2f, bodyCollider.bounds.extents.y);
        if (capsuleHalfHeight <= 0.21f) capsuleHalfHeight = 1f;

        Vector3 start = GroundedCenter(spawnPosition, spawnPosition.y + capsuleHalfHeight);
        transform.position = start;
        hasGroundY = TryGetGroundY(start, out groundY);

        if (seat != null)
            transform.rotation = FlatLookRotation(seat.position - start);

        seatedPosition = CalculateSeatedPosition();
        seatedRotation = CalculateSeatedRotation();

        PrepareNavMesh(start);

        if (bubble == null) bubble = CustomerOrderBubble.Attach(transform);
        if (bubble != null) bubble.ShowOrder(requestedDish);

        if (tray != null) tray.AssignCustomer(this);

        State = CustomerState.Entering;
        enteringTimer = 0f;
        initialized = true;

        Debug.Log("CustomerAI: spawned at " + start + ", wants " + requestedDish +
                  (navReady ? ", NavMesh hand-off at " + navEntryPoint : ", no NavMesh") + ".");
    }

    void Update()
    {
        if (!initialized || State == CustomerState.Gone) return;

        switch (State)
        {
            case CustomerState.Entering:
                UpdateEntering();
                break;

            case CustomerState.Seating:
                UpdateSeating();
                break;

            case CustomerState.WaitingForFood:
                UpdateWaiting();
                break;

            case CustomerState.Eating:
                UpdateEating();
                break;

            case CustomerState.Leaving:
                UpdateLeaving();
                break;
        }
    }

    // ------------------------------------------------------------------
    // STATES
    // ------------------------------------------------------------------

    private void UpdateEntering()
    {
        if (seat == null)
        {
            BeginSeating();
            return;
        }

        enteringTimer += Time.deltaTime;

        if (navReady)
        {
            if (usingAgent)
            {
                if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                {
                    usingAgent = false;
                }
                else if (!agent.pathPending)
                {
                    if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                        enteringTimer >= 45f)
                    {
                        StopAgent();
                        navReady = false;
                    }
                    else if (agent.remainingDistance <=
                             Mathf.Max(navArriveDistance, agent.stoppingDistance) + 0.2f &&
                             HorizontalDistance(transform.position, navSeatPoint) <= 1.1f)
                    {
                        StopAgent();
                        BeginSeating();
                        return;
                    }
                }

                if (usingAgent) return; // The agent drives the transform.
            }

            // The street outside the restaurant is a separate NavMesh island
            // from the raised restaurant floor, so the customer walks the last
            // outdoor stretch on foot and then hands over to the agent.
            if (HorizontalDistance(transform.position, navEntryPoint) <= 1.15f)
            {
                BeginAgentApproach();
                return;
            }

            MoveTowards(navEntryPoint, moveSpeed);
            return;
        }

        Bounds seatBounds = GetSeatBounds();
        Vector3 target = new Vector3(seatBounds.center.x, transform.position.y, seatBounds.center.z);

        if (HorizontalDistance(transform.position, target) <= 0.55f)
        {
            BeginSeating();
            return;
        }

        MoveTowards(target, moveSpeed);
    }

    /// <summary>
    /// Hands the remaining walk to the chair over to the NavMeshAgent. If the
    /// agent cannot be placed on the mesh the customer keeps the manual path.
    /// </summary>
    private void BeginAgentApproach()
    {
        if (!navReady || agent == null)
        {
            navReady = false;
            return;
        }

        Vector3 warp = new Vector3(transform.position.x, navEntryPoint.y, transform.position.z);
        if (!TryWarpAgent(warp) && !TryWarpAgent(navEntryPoint))
        {
            usingAgent = false;
            navReady = false;
            return;
        }

        agent.isStopped = false;
        agent.speed = Mathf.Max(0.2f, moveSpeed);
        agent.SetDestination(navSeatPoint);

        usingAgent = true;
        enteringTimer = 0f;
    }

    private void BeginSeating()
    {
        StopAgent();

        State = CustomerState.Seating;
        seatingTimer = 0f;
        seatingStart = transform.position;

        // The customer walks right up to the seat, then settles onto it.
        // Keeping the visual root slightly above the seat top reads as sitting.
        seatedPosition = CalculateSeatedPosition();
        seatedRotation = CalculateSeatedRotation();
    }

    private void UpdateSeating()
    {
        seatingTimer += Time.deltaTime;
        float t = Mathf.Clamp01(seatingTimer / 0.6f);
        float smooth = t * t * (3f - 2f * t);

        transform.position = Vector3.Lerp(seatingStart, seatedPosition, smooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, seatedRotation,
            Mathf.Clamp01(Time.deltaTime * turnSpeed));

        if (t >= 1f)
        {
            transform.position = seatedPosition;
            transform.rotation = seatedRotation;
            State = CustomerState.WaitingForFood;

            if (bubble != null) bubble.ShowOrder(requestedDish);
        }
    }

    private void UpdateWaiting()
    {
        if (tray != null && tray.currentFood != null &&
            DishMatches(tray.currentFood) && State == CustomerState.WaitingForFood)
        {
            BeginEating(tray.currentFood);
        }
    }

    private void UpdateEating()
    {
        if (eatingDish == null)
        {
            State = CustomerState.WaitingForFood;
            if (bubble != null) bubble.ShowOrder(requestedDish);
            return;
        }

        eatTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(eatTimer / Mathf.Max(0.5f, eatDuration));
        if (bubble != null) bubble.ShowEating(progress);

        if (eatTimer >= Mathf.Max(0.5f, eatDuration))
            FinishEating();
    }

    private void UpdateLeaving()
    {
        leaveTimer += Time.deltaTime;

        if (usingAgent)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                usingAgent = false;
            }
            else if (!agent.pathPending &&
                     (agent.remainingDistance <= navArriveDistance + 0.25f ||
                      agent.pathStatus != NavMeshPathStatus.PathComplete ||
                      leaveTimer >= 30f))
            {
                StopAgent();
            }
            else
            {
                return; // The agent walks the customer back toward the entry.
            }
        }

        if (exitPoint != null)
        {
            Vector3 target = new Vector3(exitPoint.position.x, transform.position.y,
                exitPoint.position.z);

            if (HorizontalDistance(transform.position, target) <= 0.8f ||
                leaveTimer >= 70f)
            {
                FinishAndDestroy();
                return;
            }

            MoveTowards(target, leaveSpeed);
        }
        else
        {
            if (seat == null || leaveTimer >= 70f)
            {
                FinishAndDestroy();
                return;
            }

            Vector3 away = transform.position - seat.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            away.Normalize();

            Vector3 target = transform.position + away * 30f;
            if (Vector3.Distance(transform.position, seat.position) >= 35f)
            {
                FinishAndDestroy();
                return;
            }

            MoveTowards(target, leaveSpeed);
        }
    }

    // ------------------------------------------------------------------
    // ORDER / EATING
    // ------------------------------------------------------------------

    public bool AcceptsDish(FoodDish dish)
    {
        return dish != null && DishMatches(dish);
    }

    public void OnDishPlaced(FoodDish dish)
    {
        if (State != CustomerState.WaitingForFood || dish == null) return;

        if (!DishMatches(dish))
        {
            ToastUI.Show("The customer ordered " + requestedDish + ", not " + dish.dishName + ".");
            if (bubble != null) bubble.ShowOrder(requestedDish);
            return;
        }

        BeginEating(dish);
    }

    private bool DishMatches(FoodDish dish)
    {
        if (dish == null) return false;

        return string.Equals(dish.dishName, requestedDish,
            StringComparison.OrdinalIgnoreCase);
    }

    private void BeginEating(FoodDish dish)
    {
        if (State != CustomerState.WaitingForFood) return;

        State = CustomerState.Eating;
        eatingDish = dish;
        eatTimer = 0f;

        if (bubble != null) bubble.ShowEating(0f);
        Debug.Log("CustomerAI: eating " + requestedDish + ".");
    }

    private void FinishEating()
    {
        int paid = Mathf.Max(0, payment);

        if (tray != null) tray.ClearCustomer(this);

        if (eatingDish != null)
        {
            eatingDish.ConsumeByCustomer();
            eatingDish = null;
        }

        MoneyManager.GetOrCreate().AddMoney(paid);

        if (bubble != null) bubble.ShowPaid(paid);

        ToastUI.Show("Customer paid $" + paid + ".");

        // Stand up before walking back to the exit. When the chair sits on the
        // NavMesh, hand the return trip to the agent so it follows the baked
        // walkways instead of sliding through furniture.
        bool agentLeaving = false;
        if (navReady && agent != null && TryWarpAgent(navSeatPoint))
        {
            agent.isStopped = false;
            agent.speed = Mathf.Max(0.2f, leaveSpeed);
            agent.SetDestination(navEntryPoint);
            usingAgent = true;
            agentLeaving = true;
        }

        if (!agentLeaving)
        {
            if (TryGetGroundY(transform.position, out groundY))
            {
                hasGroundY = true;
                transform.position = new Vector3(transform.position.x,
                    groundY + capsuleHalfHeight, transform.position.z);
            }

            usingAgent = false;
        }

        State = CustomerState.Leaving;
        leaveTimer = 0f;
    }

    private void FinishAndDestroy()
    {
        if (finished) return;

        State = CustomerState.Gone;
        finished = true;
        Finished?.Invoke(this);
        Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // MOVEMENT
    // ------------------------------------------------------------------

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        Vector3 next = Vector3.MoveTowards(transform.position, flatTarget,
            Mathf.Max(0.1f, speed) * Time.deltaTime);

        if (TryGetGroundY(next, out float foundGround))
        {
            groundY = foundGround;
            hasGroundY = true;

            float desiredY = groundY + capsuleHalfHeight;
            next.y = Mathf.MoveTowards(transform.position.y, desiredY,
                verticalFollowSpeed * Time.deltaTime);
        }

        transform.position = next;

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look,
                Mathf.Clamp01(Time.deltaTime * turnSpeed));
        }
    }

    // ------------------------------------------------------------------
    // NAVMESH
    // ------------------------------------------------------------------

    private NavMeshAgent EnsureAgent()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        float height = Mathf.Max(1f, capsuleHalfHeight * 2f);

        agent.radius = Mathf.Clamp(capsuleHalfHeight * 0.22f, 0.22f, 0.45f);
        agent.height = height;
        agent.baseOffset = -capsuleHalfHeight;
        agent.speed = Mathf.Max(0.2f, moveSpeed);
        agent.angularSpeed = 520f;
        agent.acceleration = 24f;
        agent.stoppingDistance = 0.3f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        // Keep the agent dormant while the customer walks on foot. An enabled
        // agent clamps its transform to the NavMesh and would pin the customer
        // to the edge of the street island before the hand-off point.
        agent.enabled = false;
        return agent;
    }

    /// <summary>
    /// Finds the nearest point on a complete NavMesh path to the chair. The
    /// customer walks there on foot, then the NavMeshAgent takes over. This
    /// keeps working even when the outside street and the restaurant floor are
    /// separate NavMesh islands.
    /// </summary>
    private void PrepareNavMesh(Vector3 start)
    {
        navReady = false;
        usingAgent = false;

        if (seat == null) return;

        agent = EnsureAgent();
        if (agent == null) return;

        NavMeshHit seatHit;
        if (!NavMesh.SamplePosition(seat.position, out seatHit, 8f, NavMesh.AllAreas))
            return;

        navSeatPoint = seatHit.position;

        Vector3 seatTarget = new Vector3(seat.position.x, navSeatPoint.y, seat.position.z);
        Vector3 direction = seatTarget - start;
        direction.y = 0f;
        Vector3 side = Vector3.Cross(Vector3.up, direction.normalized);

        bool found = false;
        Vector3 bestEntry = start;

        // Prefer the earliest point along the walked line that can already
        // reach the chair. Offsets catch cases where the straight line clips a
        // wall while a nearby lane is clear.
        for (float t = 0.05f; t <= 1.001f; t += 0.04f)
        {
            Vector3 probe = Vector3.Lerp(start, seatTarget, t);

            for (int s = 0; s < 5; s++)
            {
                Vector3 candidate = probe;
                if (s == 1) candidate += side * navMeshSearchDistance;
                if (s == 2) candidate -= side * navMeshSearchDistance;
                if (s == 3) candidate += side * (navMeshSearchDistance * 0.5f);
                if (s == 4) candidate -= side * (navMeshSearchDistance * 0.5f);

                NavMeshHit hit;
                if (!NavMesh.SamplePosition(candidate, out hit, 4f, NavMesh.AllAreas))
                    continue;

                if (Mathf.Abs(hit.position.y - navSeatPoint.y) > 2.6f)
                    continue;

                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(hit.position, navSeatPoint,
                        NavMesh.AllAreas, path))
                    continue;
                if (path.status != NavMeshPathStatus.PathComplete) continue;

                bestEntry = hit.position;
                found = true;
                break;
            }

            if (found) break;
        }

        if (!found) return;

        navEntryPoint = bestEntry;
        navReady = true;

        // Already standing on the connected island: hand over immediately.
        if (HorizontalDistance(transform.position, navEntryPoint) <= 1.15f)
            BeginAgentApproach();
    }

    private bool TryWarpAgent(Vector3 point)
    {
        if (agent == null) return false;

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(point, out hit, 3f, NavMesh.AllAreas)) return false;

        agent.enabled = true;
        agent.isStopped = false;

        if (!agent.Warp(hit.position))
        {
            agent.enabled = false;
            return false;
        }

        return agent.isOnNavMesh;
    }

    private void StopAgent()
    {
        usingAgent = false;

        if (agent == null) return;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        agent.enabled = false;
    }

    private Vector3 CalculateSeatedPosition()
    {
        Bounds bounds = GetSeatBounds();
        float y = bounds.max.y + capsuleHalfHeight * 0.3f;
        return new Vector3(bounds.center.x, y, bounds.center.z);
    }

    private Quaternion CalculateSeatedRotation()
    {
        if (tray != null)
        {
            Vector3 direction = tray.transform.position - seatedPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        if (seat != null)
        {
            Vector3 direction = seat.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        return transform.rotation;
    }

    private Bounds GetSeatBounds()
    {
        if (seat == null) return new Bounds(transform.position, Vector3.one);

        Collider collider = seat.GetComponentInChildren<Collider>();
        if (collider != null) return collider.bounds;

        Renderer renderer = seat.GetComponentInChildren<Renderer>();
        if (renderer != null) return renderer.bounds;

        return new Bounds(seat.position, Vector3.one);
    }

    private Vector3 GroundedCenter(Vector3 position, float fallbackY)
    {
        if (TryGetGroundY(position, out float foundGround))
            return new Vector3(position.x, foundGround + capsuleHalfHeight, position.z);

        return new Vector3(position.x, fallbackY, position.z);
    }

    /// <summary>
    /// Finds the walkable surface below the customer. Hits more than a small
    /// step above the current ground (table tops, the seat block) are ignored
    /// so the customer keeps following the floor.
    /// </summary>
    private bool TryGetGroundY(Vector3 position, out float result)
    {
        float baseY = hasGroundY ? groundY : position.y;
        Vector3 origin = new Vector3(position.x, baseY + groundProbeHeight, position.z);

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down,
            groundProbeDistance, ~0, QueryTriggerInteraction.Ignore);

        float best = float.NegativeInfinity;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;

            float hitY = hit.point.y;
            if (hasGroundY && hitY > groundY + maxStepUp) continue;
            if (hitY > best) best = hitY;
        }

        if (best == float.NegativeInfinity)
        {
            result = groundY;
            return hasGroundY;
        }

        result = best;
        return true;
    }

    private static Quaternion FlatLookRotation(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return Quaternion.identity;
        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
