using UnityEngine;

/// <summary>
/// The trader standing outside the restaurant. Interacting opens the market
/// screen, where ingredients can be bought and backpack items sold.
/// </summary>
public class MarketMan : MonoBehaviour, IInteractable
{
    [Header("Market")]
    public string marketName = "Market";

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

                float sx = Mathf.Abs(scale.x) > 0.0001f ? Mathf.Abs(scale.x) : 1f;
                float sy = Mathf.Abs(scale.y) > 0.0001f ? Mathf.Abs(scale.y) : 1f;
                float sz = Mathf.Abs(scale.z) > 0.0001f ? Mathf.Abs(scale.z) : 1f;
                box.size = new Vector3(bounds.size.x / sx, bounds.size.y / sy, bounds.size.z / sz);
            }
            else
            {
                box.size = new Vector3(1f, 2f, 1f);
                box.center = new Vector3(0f, 1f, 0f);
            }
        }
    }

    public void Interact()
    {
        MarketUI.Show(this);
    }
}
