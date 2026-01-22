using UnityEngine;
using System.Collections;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    public float holdDistance = 1.2f;
    public float holdSmooth = 15f;
    public float snapRange = 2.5f; 
    public LayerMask snapLayerMask;

    [Header("Mechanical Feel")]
    public float snapSpeed = 0.15f; // Very fast for a mechanical 'thunk'
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

        // Release the reference so the refill stops immediately on pickup
        if (currentSnapPoint != null)
        {
            currentSnapPoint.ReleaseItem();
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
        
        currentSnapPoint = snapPoint;
        StartCoroutine(DishonoredSnapRoutine(snapPoint));
    }

    private IEnumerator DishonoredSnapRoutine(SnapPoint snapPoint)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < snapSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapSpeed;
            
            // Ease-Out Cubic curve: fast start, solid finish
            float curve = 1f - Mathf.Pow(1f - t, 3f); 

            transform.position = Vector3.Lerp(startPos, snapPoint.snapPosition.position, curve);
            transform.rotation = Quaternion.Slerp(startRot, snapPoint.snapPosition.rotation, curve);
            yield return null;
        }

        transform.position = snapPoint.snapPosition.position;
        transform.rotation = snapPoint.snapPosition.rotation;
        
        // CRITICAL: This 'handshake' starts the charge logic!
        snapPoint.LockItem(this); 
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
            MoveItem();
            ScanForSnapPoints();
        }
    }

    void MoveItem()
    {
        Vector3 targetPos = playerCam.position + (playerCam.forward * holdDistance);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * holdSmooth);

        if (keepUprightWhileHeld)
        {
            Quaternion targetRot = Quaternion.Euler(0, playerCam.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * holdSmooth);
        }
    }

    void ScanForSnapPoints()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, snapRange, snapLayerMask);
        SnapPoint bestPoint = null;
        
        if (hits.Length > 0)
        {
            bestPoint = hits[0].GetComponentInParent<SnapPoint>();
            if (bestPoint == null) bestPoint = hits[0].GetComponent<SnapPoint>();
        }

        if (bestPoint != nearbySnapPoint)
        {
            if (nearbySnapPoint != null) nearbySnapPoint.SetHighlight(false);
            nearbySnapPoint = bestPoint;
            if (nearbySnapPoint != null) nearbySnapPoint.SetHighlight(true);
        }
    }
}