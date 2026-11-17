using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the restaurant customer loop: spawn one customer, wait until it leaves,
/// then wait a little and spawn the next one.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager instance;

    [Header("Timing")]
    public float firstCustomerDelay = 2f;
    public float nextCustomerDelay = 6f;

    [Header("Customer Settings")]
    public float moveSpeed = 3.2f;
    public float leaveSpeed = 4.2f;
    public float eatDuration = 9f;
    public int payment = 25;

    [Header("Order Fallback")]
    public string[] fallbackDishes =
    {
        "Abacha",
        "Egusi Soup",
        "Ofe Nsala (White Soup)",
        "Ofe Onugbu",
        "Ofe Owerri",
        "Pepper Soup",
        "Ukwa (African Breadfruit)"
    };

    private Transform spawnPoint;
    private Transform seatPoint;
    private CustomerTray customerTray;
    private CustomerAI activeCustomer;
    private bool loopStarted;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static CustomerManager GetOrCreate()
    {
        if (instance != null) return instance;

        CustomerManager existing = Object.FindAnyObjectByType<CustomerManager>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("CustomerManager");
        instance = go.AddComponent<CustomerManager>();
        return instance;
    }

    void Start()
    {
        ResolveReferences();

        if (spawnPoint == null || seatPoint == null)
        {
            Debug.LogWarning("CustomerManager: spawn point or seat was not found; " +
                             "the customer loop is disabled.");
            return;
        }

        if (!loopStarted)
        {
            loopStarted = true;
            StartCoroutine(CustomerLoop());
        }
    }

    public void ResolveReferences()
    {
        if (spawnPoint == null)
            spawnPoint = FindSceneTransform("Customer_Spawn_Point");

        if (seatPoint == null)
            seatPoint = FindSceneTransform("Customer-Seat-01");

        if (customerTray == null)
            customerTray = CustomerTray.FindByName("Customer-tray-1");

        if (customerTray == null && seatPoint != null)
            customerTray = CustomerTray.FindNearest(seatPoint.position, 3f);
    }

    private IEnumerator CustomerLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, firstCustomerDelay));

        while (true)
        {
            ResolveReferences();

            if (spawnPoint == null || seatPoint == null)
                yield break;

            if (customerTray == null)
            {
                CustomerTray.AttachToSceneTrays();
                ResolveReferences();
            }

            if (activeCustomer == null)
                SpawnCustomer();

            while (activeCustomer != null)
                yield return null;

            yield return new WaitForSeconds(Mathf.Max(0.5f, nextCustomerDelay));
        }
    }

    private void SpawnCustomer()
    {
        GameObject prefab = Resources.Load<GameObject>("Customer_01");
        if (prefab == null)
        {
            Debug.LogWarning("CustomerManager: Resources/Customer_01.prefab was not found. " +
                             "Using a procedural fallback customer.");
            prefab = CreateFallbackCustomer();
        }

        GameObject customer = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        customer.name = "Customer_01";

        CustomerAI ai = customer.GetComponent<CustomerAI>();
        if (ai == null) ai = customer.AddComponent<CustomerAI>();

        ai.moveSpeed = moveSpeed;
        ai.leaveSpeed = leaveSpeed;
        ai.eatDuration = eatDuration;
        ai.payment = payment;

        ai.Finished += HandleCustomerFinished;
        ai.Initialize(seatPoint, spawnPoint, customerTray, PickDish(), spawnPoint.position);

        activeCustomer = ai;
    }

    private void HandleCustomerFinished(CustomerAI customer)
    {
        if (activeCustomer == customer)
            activeCustomer = null;
    }

    private string PickDish()
    {
        List<string> names = new List<string>();

        if (PlayerInventory.instance != null &&
            PlayerInventory.instance.unlockedRecipes != null &&
            PlayerInventory.instance.unlockedRecipes.Count > 0)
        {
            foreach (Recipe recipe in PlayerInventory.instance.unlockedRecipes)
            {
                if (recipe == null || recipe.finishedDishPrefab == null) continue;
                if (!string.IsNullOrEmpty(recipe.recipeName) && !names.Contains(recipe.recipeName))
                    names.Add(recipe.recipeName);
            }
        }

        if (names.Count == 0)
        {
            Recipe[] loaded = Resources.FindObjectsOfTypeAll<Recipe>();
            foreach (Recipe recipe in loaded)
            {
                if (recipe == null || recipe.finishedDishPrefab == null) continue;
                if (!string.IsNullOrEmpty(recipe.recipeName) && !names.Contains(recipe.recipeName))
                    names.Add(recipe.recipeName);
            }
        }

        if (names.Count == 0 && fallbackDishes != null)
        {
            foreach (string dish in fallbackDishes)
                if (!string.IsNullOrEmpty(dish)) names.Add(dish);
        }

        if (names.Count == 0) return "Abacha";
        return names[Random.Range(0, names.Count)];
    }

    private static GameObject CreateFallbackCustomer()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Customer_01";

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.20f, 0.55f, 0.62f);
                renderer.material = material;
            }
        }

        return go;
    }

    private static Transform FindSceneTransform(string name)
    {
        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include);

        foreach (Transform t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            if (t.name == name) return t;
        }

        return null;
    }
}
