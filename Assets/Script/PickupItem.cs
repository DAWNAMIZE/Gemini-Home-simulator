using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    public float holdDistance = 2f;
    public float holdSmooth = 10f;
    public float snapRange = 1.2f;
    public LayerMask snapLayerMask;

    private Transform playerCam;
    private Rigidbody rb;
    private bool isHeld = false;
    private Transform originalParent;
    private SnapPoint currentSnapPoint;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }

    public void Interact()
    {
        // Lazy assign camera
        if (playerCam == null)
            playerCam = Camera.main.transform;

        if (!isHeld)
        {
            // Try picking up
            PickUp();
        }
        else
        {
            // Try snapping or dropping
            TryPlaceOrDrop();
        }
    }

    void PickUp()
    {
        isHeld = true;
        originalParent = transform.parent;
        rb.useGravity = false;
        rb.isKinematic = true;
        transform.parent = playerCam;
    }

    void TryPlaceOrDrop()
    {
        // Check if near a SnapPoint
        Collider[] hits = Physics.OverlapSphere(transform.position, snapRange, snapLayerMask);

        if (hits.Length > 0)
        {
            // Snap to nearest point
            SnapPoint snapPoint = hits[0].GetComponent<SnapPoint>();
            if (snapPoint != null && !snapPoint.IsOccupied)
            {
                SnapToPoint(snapPoint);
                return;
            }
        }

        // If no snap point found → Drop to ground
        Drop();
    }

    void SnapToPoint(SnapPoint snapPoint)
    {
        isHeld = false;
        transform.parent = null;
        transform.position = snapPoint.snapPosition.position;
        transform.rotation = snapPoint.snapPosition.rotation;
        rb.isKinematic = true;
        rb.useGravity = false;
        snapPoint.IsOccupied = true;
        snapPoint.CurrentItem = this;
        currentSnapPoint = snapPoint;
    }

    void Drop()
    {
        isHeld = false;
        transform.parent = null;
        rb.isKinematic = false;
        rb.useGravity = true;

        // Free any snap point if previously used
        if (currentSnapPoint != null)
        {
            currentSnapPoint.IsOccupied = false;
            currentSnapPoint.CurrentItem = null;
            currentSnapPoint = null;
        }
    }

    void Update()
    {
        if (isHeld && playerCam != null)
        {
            Vector3 targetPos = playerCam.position + playerCam.forward * holdDistance;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * holdSmooth);
        }
    }
}
