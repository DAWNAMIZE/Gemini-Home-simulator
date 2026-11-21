using UnityEngine;

/// <summary>
/// The armory trader. Interacting opens the weapons, ammo and trap catalogue.
/// </summary>
public class ArmoryMan : MonoBehaviour, IInteractable
{
    [Header("Armory")]
    public string shopName = "Armory";

    void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                Vector3 scale = transform.lossyScale;
                box.center = transform.InverseTransformPoint(bounds.center);
                box.size = new Vector3(
                    bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                    bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                    bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            }
            else
            {
                box.center = new Vector3(0f, 1f, 0f);
                box.size = new Vector3(1f, 2f, 0.6f);
            }
        }
    }

    public void Interact()
    {
        ArmoryUI.Show(this);
    }
}
