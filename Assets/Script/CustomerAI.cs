using System;
using UnityEngine;

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
    private bool initialized;
    private bool finished;

    void Awake()
    {
        bodyCollider = GetComponent<CapsuleCollider>();
        if (bodyCollider != null)
            capsuleHalfHeight = Mathf.Max(0.2f, bodyCollider.bounds.extents.y);

        if (capsuleHalfHeight <= 0.21f)
            capsuleHalfHeight = 1f;
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

        if (bubble == null) bubble = CustomerOrderBubble.Attach(transform);
        if (bubble != null) bubble.ShowOrder(requestedDish);

        if (tray != null) tray.AssignCustomer(this);

        State = CustomerState.Entering;
        initialized = true;

        Debug.Log("CustomerAI: spawned at " + start + ", wants " + requestedDish + ".");
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

        Bounds seatBounds = GetSeatBounds();
        Vector3 target = new Vector3(seatBounds.center.x, transform.position.y, seatBounds.center.z);

        if (HorizontalDistance(transform.position, target) <= 0.55f)
        {
            BeginSeating();
            return;
        }

        MoveTowards(target, moveSpeed);
    }

    private void BeginSeating()
    {
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

        // Stand up before walking back to the exit.
        if (TryGetGroundY(transform.position, out groundY))
        {
            hasGroundY = true;
            transform.position = new Vector3(transform.position.x,
                groundY + capsuleHalfHeight, transform.position.z);
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
