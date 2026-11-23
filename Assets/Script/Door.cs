using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public Transform hinge;            // The pivot/hinge point of the door
    public float openAngle = 90f;      // How far the door swings open (degrees)
    public float openSpeed = 2f;       // Speed of door animation
    public bool isOpen = false;        // Current open/closed state
    public float autoCloseDelay = 3f;  // Seconds before door auto-closes (0 = disable)

    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;
    public bool breakable = true;
    public bool openOnBreak = true;

    [Header("Enemy Navigation")]
    [Tooltip("Extra distance from the door used when an enemy stands in front of it.")]
    public float navStandOff = 1.5f;

    private static readonly List<Door> allDoors = new List<Door>();
    private static int nextHoldId;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isAnimating;
    private bool isBroken;
    private Coroutine animationRoutine;
    private Coroutine autoCloseRoutine;

    private readonly HashSet<int> openHolds = new HashSet<int>();

    // Door-plane geometry, captured while the door is still closed.
    private bool geometryCached;
    private Vector3 doorCenter;
    private Vector3 doorAlong;
    private Vector3 doorNormal;
    private float doorHalfWidth = 0.75f;

    private NavMeshLink navLink;
    private int navLinkAgentType = int.MinValue;

    public static IReadOnlyList<Door> AllDoors => allDoors;

    public bool IsOpen => isOpen;
    public bool IsClosed => !isOpen && !isBroken;
    public bool IsBroken => isBroken;

    /// <summary>True when an agent can walk through the doorway right now.</summary>
    public bool IsPassable => !isAnimating && (isBroken ? openOnBreak : isOpen);

    public float Health => health;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

    /// <summary>Middle of the doorway opening, in world space.</summary>
    public Vector3 Center
    {
        get
        {
            CacheGeometry();
            return doorCenter;
        }
    }

    void Awake()
    {
        if (hinge == null)
            hinge = transform;

        closedRotation = hinge.localRotation;
        openRotation = hinge.localRotation * Quaternion.Euler(0f, openAngle, 0f);

        if (maxHealth <= 0f) maxHealth = 1f;
        health = Mathf.Clamp(health, 0f, maxHealth);

        // Must be captured before any rotation is applied.
        CacheGeometry();
    }

    void Start()
    {
        // A door saved in the open state should already be swung open.
        if (isOpen && !isBroken)
            hinge.localRotation = openRotation;
    }

    void OnEnable()
    {
        if (!allDoors.Contains(this))
            allDoors.Add(this);
    }

    void OnDisable()
    {
        allDoors.Remove(this);
    }

    void OnDestroy()
    {
        allDoors.Remove(this);

        if (navLink != null)
        {
            Destroy(navLink.gameObject);
            navLink = null;
        }
    }

    public void Interact()
    {
        if (isBroken) return;
        SetOpen(!isOpen);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool open, bool instant = false)
    {
        if (isBroken) return;

        if (isAnimating)
            StopAnimation();

        isOpen = open;

        if (!open)
            StopAutoClose();

        if (instant || openSpeed <= 0f)
        {
            hinge.localRotation = open ? openRotation : closedRotation;
            UpdateNavLink();

            if (open)
                StartAutoCloseIfNeeded();
            return;
        }

        animationRoutine = StartCoroutine(AnimateTo(open ? openRotation : closedRotation));
    }

    private IEnumerator AnimateTo(Quaternion targetRotation)
    {
        isAnimating = true;
        Quaternion startRotation = hinge.localRotation;
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * Mathf.Max(0.01f, openSpeed);
            hinge.localRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Clamp01(progress));
            yield return null;
        }

        hinge.localRotation = targetRotation;
        animationRoutine = null;
        isAnimating = false;
        UpdateNavLink();

        if (isOpen)
            StartAutoCloseIfNeeded();
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        isAnimating = false;
    }

    private void StartAutoCloseIfNeeded()
    {
        StopAutoClose();

        if (autoCloseDelay > 0f && isOpen && !isBroken && openHolds.Count == 0)
            autoCloseRoutine = StartCoroutine(AutoCloseAfterDelay());
    }

    private void StopAutoClose()
    {
        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        autoCloseRoutine = null;

        if (isOpen && !isBroken && !isAnimating && openHolds.Count == 0)
            SetOpen(false);
    }

    /// <summary>
    /// Keeps the door open until the returned handle is released. Used by an
    /// enemy that is walking through the doorway.
    /// </summary>
    public int AcquireOpenHold()
    {
        int holdId = ++nextHoldId;
        openHolds.Add(holdId);
        StopAutoClose();

        if (!isBroken && !isOpen)
            SetOpen(true);

        return holdId;
    }

    public void ReleaseOpenHold(int holdId)
    {
        if (!openHolds.Remove(holdId)) return;

        if (openHolds.Count == 0 && isOpen && !isBroken && autoCloseDelay > 0f)
            StartAutoCloseIfNeeded();
    }

    public void TakeDamage(float amount)
    {
        if (!breakable || isBroken || amount <= 0f) return;

        health = Mathf.Max(0f, health - amount);

        if (health <= 0f)
            BreakDoor();
    }

    private void BreakDoor()
    {
        isBroken = true;
        health = 0f;
        StopAnimation();
        StopAutoClose();

        if (openOnBreak)
        {
            // The door is smashed out of the way. Move it immediately so the
            // collider is no longer blocking the doorway.
            isOpen = true;
            hinge.localRotation = openRotation;
        }

        UpdateNavLink();
    }

    /// <summary>True when the two positions are on opposite sides of the door plane.</summary>
    public bool Separates(Vector3 a, Vector3 b)
    {
        float distanceA = SignedDistance(a);
        float distanceB = SignedDistance(b);

        if (Mathf.Abs(distanceA) < 0.05f || Mathf.Abs(distanceB) < 0.05f)
            return false;

        return (distanceA > 0f) != (distanceB > 0f);
    }

    public float SignedDistance(Vector3 position)
    {
        CacheGeometry();
        Vector3 offset = position - doorCenter;
        offset.y = 0f;
        return Vector3.Dot(offset, doorNormal);
    }

    /// <summary>Horizontal distance from a position to the door panel.</summary>
    public float HorizontalDistanceTo(Vector3 position)
    {
        CacheGeometry();

        Vector3 offset = position - doorCenter;
        offset.y = 0f;

        float planeDistance = Mathf.Abs(Vector3.Dot(offset, doorNormal));
        float lateralDistance = Mathf.Abs(Vector3.Dot(offset, doorAlong)) - doorHalfWidth;
        if (lateralDistance < 0f) lateralDistance = 0f;

        return Mathf.Sqrt(planeDistance * planeDistance + lateralDistance * lateralDistance);
    }

    /// <summary>Point to stand on while opening/attacking the door, on the caller's side.</summary>
    public Vector3 GetApproachPoint(Vector3 from)
    {
        return GetSidePoint(from, 1f);
    }

    /// <summary>Point past the doorway on the side opposite the caller.</summary>
    public Vector3 GetExitPoint(Vector3 from)
    {
        return GetSidePoint(from, -1f);
    }

    private Vector3 GetSidePoint(Vector3 from, float sideMultiplier)
    {
        CacheGeometry();

        float side = SignedDistance(from) >= 0f ? 1f : -1f;
        side *= sideMultiplier;

        float standOff = Mathf.Max(1f, navStandOff + doorHalfWidth);
        Vector3 point = doorCenter + doorNormal * (side * standOff);
        point.y = doorCenter.y;

        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            // Never let the sampled point jump through a closed door.
            if (SignedDistance(hit.position) * side >= -0.05f)
                point = hit.position;
            else
                point.y = hit.position.y;
        }

        return point;
    }

    /// <summary>
    /// Makes sure a runtime NavMesh link spans this doorway. The scene bake may
    /// have been generated with the doors closed, which leaves the two sides of
    /// the fence as separate NavMesh islands.
    /// </summary>
    public void EnsureNavLink(int agentTypeID)
    {
        CacheGeometry();

        if (navLink != null)
        {
            if (navLinkAgentType != agentTypeID)
            {
                navLinkAgentType = agentTypeID;
                navLink.agentTypeID = agentTypeID;
            }

            navLink.activated = IsPassable;
            return;
        }

        Vector3 start = doorCenter - doorNormal * 0.8f;
        Vector3 end = doorCenter + doorNormal * 0.8f;

        if (NavMesh.SamplePosition(start, out NavMeshHit startHit, 15f, NavMesh.AllAreas))
        {
            // Keep each endpoint on its own side of the doorway.
            if (SignedDistance(startHit.position) <= 0.05f)
                start = startHit.position;
            else
                start.y = startHit.position.y;
        }

        if (NavMesh.SamplePosition(end, out NavMeshHit endHit, 15f, NavMesh.AllAreas))
        {
            if (SignedDistance(endHit.position) >= -0.05f)
                end = endHit.position;
            else
                end.y = endHit.position.y;
        }

        GameObject linkObject = new GameObject("DoorNavLink (" + name + ")");
        linkObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        navLinkAgentType = agentTypeID;
        navLink = linkObject.AddComponent<NavMeshLink>();
        navLink.agentTypeID = agentTypeID;
        navLink.startPoint = start;
        navLink.endPoint = end;
        navLink.width = Mathf.Clamp(doorHalfWidth * 1.6f, 0.8f, 4f);
        navLink.bidirectional = true;
        navLink.autoUpdate = false;
        navLink.activated = IsPassable;
    }

    private void UpdateNavLink()
    {
        if (navLink != null)
            navLink.activated = IsPassable;
    }

    private void CacheGeometry()
    {
        if (geometryCached) return;
        geometryCached = true;

        Transform pivot = hinge != null ? hinge : transform;
        Renderer[] renderers = pivot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Vector3 pivotPosition = pivot.position;

        if (hasBounds)
        {
            doorCenter = bounds.center;

            Vector3 along = doorCenter - pivotPosition;
            along.y = 0f;

            if (along.sqrMagnitude < 0.01f)
                along = pivot.right;

            along.y = 0f;

            if (along.sqrMagnitude < 0.0001f)
                along = Vector3.forward;

            doorAlong = along.normalized;

            Vector3 absoluteAlong = new Vector3(Mathf.Abs(doorAlong.x), 0f, Mathf.Abs(doorAlong.z));
            doorHalfWidth = Mathf.Max(0.3f, Vector3.Dot(bounds.extents, absoluteAlong));
        }
        else
        {
            doorCenter = pivotPosition;

            doorAlong = pivot.right;
            doorAlong.y = 0f;
            if (doorAlong.sqrMagnitude < 0.0001f)
                doorAlong = Vector3.forward;
            doorAlong.Normalize();

            doorHalfWidth = 0.75f;
        }

        doorNormal = Vector3.Cross(Vector3.up, doorAlong);
        if (doorNormal.sqrMagnitude < 0.0001f)
            doorNormal = Vector3.forward;
        doorNormal.Normalize();
    }

    /// <summary>Nearest active door to a position, regardless of which side it is on.</summary>
    public static Door FindNearestDoor(Vector3 position)
    {
        Door nearest = null;
        float bestSqrDistance = float.PositiveInfinity;

        foreach (Door door in allDoors)
        {
            if (door == null || !door.isActiveAndEnabled) continue;

            float sqrDistance = (door.Center - position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = door;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Nearest door whose plane separates the two positions. This is the door
    /// an enemy must pass through to reach the target.
    /// </summary>
    public static Door FindDoorBetween(Vector3 from, Vector3 to)
    {
        Door nearest = null;
        float bestSqrDistance = float.PositiveInfinity;

        foreach (Door door in allDoors)
        {
            if (door == null || !door.isActiveAndEnabled) continue;
            if (!door.Separates(from, to)) continue;

            float sqrDistance = (door.Center - from).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = door;
            }
        }

        return nearest;
    }

    public string GetNameForUI()
    {
        return "Door";
    }
}
