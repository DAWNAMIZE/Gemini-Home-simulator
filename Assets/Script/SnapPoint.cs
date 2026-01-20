using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("Configuration")]
    public Transform snapPosition;  // The deep part of the socket
    public bool IsOccupied = false;
    public PickupItem CurrentItem;

    [Header("Visual Feedback")]
    public GameObject ghostPreview; 
    public MeshRenderer highlightRenderer; 
    public Color highlightColor = new Color(0, 1, 1, 0.5f); // Cyan glow
    private Color originalColor;

    void Start()
    {
        if (ghostPreview != null) ghostPreview.SetActive(false);
        if (highlightRenderer != null) 
            originalColor = highlightRenderer.material.color;
    }

    public void SetHighlight(bool active)
    {
        if (IsOccupied) 
        {
            if (ghostPreview != null) ghostPreview.SetActive(false);
            return;
        }

        if (ghostPreview != null) ghostPreview.SetActive(active);

        if (highlightRenderer != null)
        {
            highlightRenderer.material.color = active ? highlightColor : originalColor;
        }
    }

    // This allows the machine to "lock" the canister in place
    public void LockItem()
    {
        IsOccupied = true;
        // You can add a sound effect here: "Clunk!"
        Debug.Log("Mechanical Lock Engaged.");
    }

    void OnDrawGizmos()
    {
        if (snapPosition == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(snapPosition.position, new Vector3(0.2f, 0.4f, 0.2f));
        Gizmos.DrawRay(snapPosition.position, snapPosition.up * 0.5f);
    }
}