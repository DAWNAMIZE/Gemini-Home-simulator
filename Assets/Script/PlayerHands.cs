using UnityEngine;

public class PlayerHands : MonoBehaviour
{
    public static PlayerHands instance;

    [Header("Holding Setup")]
    public Transform holdPosition;  
    public GameObject dropButtonUI; 

    public enum HoldType { None, Pot, Canister }
    
    [Header("Current Item")]
    public HoldType currentHoldType = HoldType.None;
    public PortablePot heldPot;
    
    // THIS is the exact variable Unity was looking for!
    public GasCanister heldCanister; 

    public bool isHoldingPot => currentHoldType == HoldType.Pot;
    public bool isHoldingCanister => currentHoldType == HoldType.Canister;

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

    public void GrabCanister(GasCanister canister)
    {
        currentHoldType = HoldType.Canister;
        heldCanister = canister;
        SetupHeldObject(canister.gameObject);
    }

    private void SetupHeldObject(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (holdPosition != null)
        {
            obj.transform.SetParent(holdPosition);
            obj.transform.localPosition = Vector3.zero;       
            obj.transform.localRotation = Quaternion.identity; 
            obj.transform.localScale = Vector3.one; 
            obj.SetActive(true); // Forces the object to be visible!
        }

        if (dropButtonUI != null) dropButtonUI.SetActive(true);
    }

    public void Release()
    {
        currentHoldType = HoldType.None;
        heldPot = null;
        heldCanister = null;
        if (dropButtonUI != null) dropButtonUI.SetActive(false);
    }

    public void ManualDrop()
    {
        if (currentHoldType == HoldType.Pot && heldPot != null) DropObject(heldPot.gameObject);
        else if (currentHoldType == HoldType.Canister && heldCanister != null) DropObject(heldCanister.gameObject);
    }

    private void DropObject(GameObject obj)
    {
        obj.transform.SetParent(null);
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (Camera.main != null)
        {
            obj.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
        }

        Release(); 
    }
}