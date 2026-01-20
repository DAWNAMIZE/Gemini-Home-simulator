using UnityEngine;
using System.Collections;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    public float holdDistance = 1.2f;
    public float holdSmooth = 15f;
    public float snapRange = 2.5f; 
    public LayerMask snapLayerMask;

    [Header("Dishonored Style Snapping")]
    public float snapSpeed = 0.2f; // Very fast for that mechanical feel
    public bool keepUprightWhileHeld = true;

    private Transform playerCam;
    private Rigidbody rb;
    private bool isHeld = false;
    private SnapPoint currentSnapPoint;
    private SnapPoint nearbySnapPoint;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
    }

    public void Interact()
    {
        if (playerCam == null) playerCam = Camera.main.transform;

        if (!isHeld) PickUp();
        else TryPlaceOrDrop();
    }

    void PickUp()
    {
        isHeld = true;
        rb.useGravity = false;
        rb.isKinematic = true;
        transform.parent = playerCam;

        if (currentSnapPoint != null)
        {
            currentSnapPoint.IsOccupied = false;
            currentSnapPoint.CurrentItem = null;
            currentSnapPoint = null;
        }
    }

    void TryPlaceOrDrop()
    {
        if (nearbySnapPoint != null && !nearbySnapPoint.IsOccupied)
        {
            SnapToPoint(nearbySnapPoint);
            nearbySnapPoint.SetHighlight(false);
            nearbySnapPoint = null;
            return;
        }
        Drop();
    }

    void SnapToPoint(SnapPoint snapPoint)
    {
        isHeld = false;
        transform.parent = null;
        rb.isKinematic = true;
        
        StartCoroutine(DishonoredSnapRoutine(snapPoint));

        snapPoint.CurrentItem = this;
        currentSnapPoint = snapPoint;
    }

    private IEnumerator DishonoredSnapRoutine(SnapPoint snapPoint)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // Final target values
        Vector3 targetPos = snapPoint.snapPosition.position;
        Quaternion targetRot = snapPoint.snapPosition.rotation;

        while (elapsed < snapSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapSpeed;

            // "Snappy" curve: starts fast, ends with a solid stop
            float curve = 1f - Mathf.Pow(1f - t, 3f); 

            transform.position = Vector3.Lerp(startPos, targetPos, curve);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curve);
            
            yield return null;
        }

        // Final placement
        transform.position = targetPos;
        transform.rotation = targetRot;

        // Tell the socket to "Lock" the item
        snapPoint.LockItem();
    }

    void Drop()
    {
        isHeld = false;
        transform.parent = null;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void Update()
    {
        if (isHeld && playerCam != null)
        {
            MoveAndRotateHeldItem();
            ScanForSnapPoints();
        }
    }

    void MoveAndRotateHeldItem()
    {
        // Smooth position follow
        Vector3 targetPos = playerCam.position + (playerCam.forward * holdDistance);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * holdSmooth);

        // Rotation logic: Keep vertical but follow player's look direction
        if (keepUprightWhileHeld)
        {
            Quaternion targetRot = Quaternion.Euler(0, playerCam.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * holdSmooth);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, playerCam.rotation, Time.deltaTime * holdSmooth);
        }
    }

    void ScanForSnapPoints()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, snapRange, snapLayerMask);
        SnapPoint bestPoint = null;

        if (hits.Length > 0)
        {
            bestPoint = hits[0].GetComponent<SnapPoint>();
        }

        if (bestPoint != nearbySnapPoint)
        {
            if (nearbySnapPoint != null) nearbySnapPoint.SetHighlight(false);
            nearbySnapPoint = bestPoint;
            if (nearbySnapPoint != null) nearbySnapPoint.SetHighlight(true);
        }
    }
}