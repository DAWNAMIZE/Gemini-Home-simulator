using UnityEngine;

// The Corridors Animation Series - Core Interaction
public class PlayerHands : MonoBehaviour
{
    public static PlayerHands instance;

    [Header("Holding Setup")]
    public Transform holdPosition;  
    public GameObject dropButtonUI; 

    // Stripped out the old Gas Canister logic
    public enum HoldType { None, Pot } 
    
    [Header("Current Item")]
    public HoldType currentHoldType = HoldType.None;
    public PortablePot heldPot;

    public bool isHoldingPot => currentHoldType == HoldType.Pot;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (dropButtonUI != null) dropButtonUI.SetActive(false);
    }

    public void GrabPot(PortablePot pot)
    {
        currentHoldType = HoldType.Pot;
        heldPot = pot;
        SetupHeldObject(pot.gameObject);
    }

    private void SetupHeldObject(GameObject obj)
    {
        // 1. Disable the collider so it doesn't bump the player
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. THE GRAVITY FIX: Freeze physics so it doesn't fall through the floor!
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; 

        // 3. THE RAYCAST FIX: Move to the "Ignore Raycast" layer (Layer 2) 
        // so your interaction laser passes through it and hits the oven!
        obj.layer = 2; 

        if (holdPosition != null)
        {
            obj.transform.SetParent(holdPosition);
            obj.transform.localPosition = Vector3.zero;       
            obj.transform.localRotation = Quaternion.identity; 
            
            // 4. THE ANTI-SHRINK FIX: Forces the item to stay its normal size
            obj.transform.localScale = Vector3.one; 
            
            obj.SetActive(true); 
        }

        if (dropButtonUI != null) dropButtonUI.SetActive(true);
    }

    public void Release()
    {
        currentHoldType = HoldType.None;
        heldPot = null;
        if (dropButtonUI != null) dropButtonUI.SetActive(false);
    }

    public void ManualDrop()
    {
        if (currentHoldType == HoldType.Pot && heldPot != null) DropObject(heldPot.gameObject);
    }

    private void DropObject(GameObject obj)
    {
        obj.transform.SetParent(null);
        
        // 1. Turn the collider back on
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // 2. Unfreeze physics so it falls back to the ground
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false; 

        // 3. Return the object to the Default layer (Layer 0) so it can be interacted with again
        obj.layer = 0; 

        if (Camera.main != null)
        {
            obj.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
        }

        Release(); 
    }
}