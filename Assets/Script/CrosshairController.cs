using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("Settings")]
    public float interactDistance = 3f; // Match your PlayerInteract range
    public LayerMask interactableLayer;
    
    [Header("UI References")]
    public Image crosshairImage;
    public Color defaultColor = Color.white;
    public Color interactColor = Color.green;

    private Transform camTransform;

    void Start()
    {
        camTransform = Camera.main.transform;
        if (crosshairImage == null) crosshairImage = GetComponent<Image>();
    }

    void Update()
    {
        CheckInteraction();
    }

    void CheckInteraction()
    {
        // Fire a ray from the center of the screen
        Ray ray = new Ray(camTransform.position, camTransform.forward);
        RaycastHit hit;

        // Check if we hit something within range
        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            // Check if the object has an interactable component
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            // Special check for doors as they often have components on parents
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                crosshairImage.color = interactColor;
                return;
            }
        }

        // If nothing found, reset color
        crosshairImage.color = defaultColor;
    }
}