using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("Snap Point")]
    public Transform snapPosition;  // Where the item sits
    public bool IsOccupied = false;
    public PickupItem CurrentItem;

    void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        if (snapPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, snapPosition.position);
        }
    }
}
