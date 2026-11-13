using UnityEngine;

/// <summary>
/// Optional pose override for physical items (wood, tools, etc.) that get
/// parented to the player's HoldPosition. The original local scale/rotation is
/// captured so dropping the item restores its world appearance.
/// </summary>
public class HeldItemPose : MonoBehaviour
{
    [Header("Held Pose")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEuler = Vector3.zero;
    public Vector3 localScale = Vector3.one;

    private Vector3 originalLocalScale = Vector3.one;
    private Quaternion originalLocalRotation = Quaternion.identity;
    private bool captured;

    void Awake()
    {
        Capture();
    }

    public void Capture()
    {
        if (captured) return;

        originalLocalScale = transform.localScale;
        originalLocalRotation = transform.localRotation;
        captured = true;
    }

    public void Apply(Transform target)
    {
        if (target == null) return;
        Capture();

        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEuler);
        target.localScale = localScale;
    }

    public void Restore(Transform target)
    {
        if (target == null) return;
        Capture();

        target.localRotation = originalLocalRotation;
        target.localScale = originalLocalScale;
    }
}
