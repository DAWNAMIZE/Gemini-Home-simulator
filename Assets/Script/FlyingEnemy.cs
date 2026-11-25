using UnityEngine;

/// <summary>
/// Sphere-shaped flying enemy released by a SporeHive.
///
/// Behaviour:
///  1. Pick the nearest window (prefer one that is boarded up).
///  2. Hover outside it and spit spores at the barricade until all ten hits
///     are gone.
///  3. Fly through the window and shoot at the player from inside the house.
///
/// The flier never interacts with doors. It only knows about windows, the
/// player, and its own hive.
/// </summary>
[DisallowMultipleComponent]
public class FlyingEnemy : MonoBehaviour
{
    private enum FlightState
    {
        SeekWindow,
        BreakBarricade,
        EnterWindow,
        AttackPlayer,
        Dead
    }

    [Header("Flight")]
    public float moveSpeed = 3.2f;
    public float turnSpeed = 7f;
    public float bobHeight = 0.12f;
    public float bobSpeed = 2.4f;

    [Header("Window Breach")]
    [Tooltip("How far outside the window the flier hovers while shooting.")]
    public float windowApproachDistance = 3f;

    [Tooltip("Distance at which the flier stops approaching the window and starts shooting.")]
    public float windowApproachTolerance = 0.65f;

    [Tooltip("How far past the window the flier travels before it starts hunting.")]
    public float entryDepth = 2.2f;

    [Header("Combat")]
    public float playerAttackRange = 7f;
    public float playerHoverDistance = 4.2f;
    public float barricadeFireInterval = 0.55f;
    public float playerFireInterval = 1.4f;
    public float projectileForce = 22f;
    public float projectileDamage = 8f;
    public int barricadeDamagePerHit = 1;

    [Header("Health")]
    public int health = 60;

    [Header("Visual")]
    public float bodyScale = 0.8f;
    public Color bodyColor = new Color(0.38f, 0.66f, 0.20f);
    public Color glowColor = new Color(0.45f, 1f, 0.22f);

    [HideInInspector] public SporeHive hive;

    private FlightState state = FlightState.SeekWindow;
    private WindowRepairPoint targetWindow;
    private WoodenBarricade barricade;
    private Transform player;
    private Transform firePoint;
    private Transform visualRoot;
    private Vector3 visualBasePosition;

    private Vector3 windowCenter;
    private Vector3 outward;
    private Vector3 inward;
    private Vector3 hoverPoint;
    private Vector3 entryPoint;
    private Vector3 insidePoint;

    private bool reachedEntry;
    private bool enteredHouse;
    private bool glassBroken;
    private float nextFireTime;
    private float bobPhase;
    private float orbitOffset;

    public bool HasEnteredHouse => enteredHouse;

    void Awake()
    {
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        BuildRuntimeParts();
    }

    void Start()
    {
        orbitOffset = Random.Range(0f, 360f);
        AcquirePlayer();
        ChooseWindow();
    }

    void Update()
    {
        if (state == FlightState.Dead) return;

        if (player == null) AcquirePlayer();
        ApplyBob();

        switch (state)
        {
            case FlightState.SeekWindow:
                UpdateSeekWindow();
                break;

            case FlightState.BreakBarricade:
                UpdateBreakBarricade();
                break;

            case FlightState.EnterWindow:
                UpdateEnterWindow();
                break;

            case FlightState.AttackPlayer:
                UpdateAttackPlayer();
                break;
        }
    }

    // ------------------------------------------------------------------
    // SETUP
    // ------------------------------------------------------------------

    private void BuildRuntimeParts()
    {
        bool hasRenderer = GetComponentsInChildren<Renderer>(true).Length > 0;

        if (!hasRenderer)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "SporeBall";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * bodyScale;

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                visualCollider.enabled = false;
                Destroy(visualCollider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = CreateGlowMaterial(bodyColor, glowColor);

            visualRoot = visual.transform;
        }
        else
        {
            Transform visual = transform.Find("Visual");
            visualRoot = visual != null ? visual : transform;
        }

        if (visualRoot != null) visualBasePosition = visualRoot.localPosition;

        if (GetComponent<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = Mathf.Max(0.2f, bodyScale * 0.62f);
        }

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        firePoint = transform.Find("FirePoint");
        if (firePoint == null)
        {
            GameObject point = new GameObject("FirePoint");
            point.transform.SetParent(transform, false);
            point.transform.localPosition = new Vector3(0f, 0f, 0.85f);
            firePoint = point.transform;
        }

        AttachSporeGlow();

        // A short trail makes the primitive sphere easy to track in flight.
        if (GetComponent<TrailRenderer>() == null && visualRoot != null)
        {
            TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0f;
            trail.numCapVertices = 2;
            trail.sharedMaterial = CreateTrailMaterial(glowColor);
            trail.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.8f);
            trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        }
    }

    private void AttachSporeGlow()
    {
        if (GetComponentInChildren<Light>(true) != null) return;

        GameObject prefab = Resources.Load<GameObject>("SporeGlow");
        if (prefab != null)
        {
            GameObject glow = Instantiate(prefab, transform, false);
            glow.name = "SporeGlow";
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localRotation = Quaternion.identity;
            return;
        }

        // Fallback for scenes or builds where the prefab was not imported.
        GameObject runtimeGlow = new GameObject("SporeGlow");
        runtimeGlow.transform.SetParent(transform, false);

        Light light = runtimeGlow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = 3.5f;
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;
    }

    private void AcquirePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            return;
        }

        PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>();
        if (vitals != null) player = vitals.transform;
    }

    private void ChooseWindow()
    {
        WindowRepairPoint[] windows = FindObjectsByType<WindowRepairPoint>(
            FindObjectsInactive.Exclude);

        if (windows == null || windows.Length == 0)
        {
            // No window in this scene: go straight for the player.
            enteredHouse = true;
            state = FlightState.AttackPlayer;
            return;
        }

        WindowRepairPoint nearest = null;
        float nearestDistance = float.MaxValue;
        WindowRepairPoint nearestBarricaded = null;
        float nearestBarricadeDistance = float.MaxValue;

        foreach (WindowRepairPoint window in windows)
        {
            if (window == null) continue;

            float distance = Vector3.Distance(transform.position, window.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = window;
            }

            if (window.barricade != null && distance < nearestBarricadeDistance)
            {
                nearestBarricadeDistance = distance;
                nearestBarricaded = window;
            }
        }

        // If the player has boarded anything up, that window is the target.
        targetWindow = nearestBarricaded != null ? nearestBarricaded : nearest;
        if (targetWindow == null)
        {
            enteredHouse = true;
            state = FlightState.AttackPlayer;
            return;
        }

        inward = targetWindow.transform.forward;
        inward.y = 0f;

        if (inward.sqrMagnitude < 0.001f)
        {
            Vector3 towardHouse = (player != null ? player.position : transform.position + Vector3.forward * 10f)
                - targetWindow.transform.position;
            towardHouse.y = 0f;
            inward = towardHouse.sqrMagnitude > 0.001f ? towardHouse.normalized : Vector3.forward;
        }
        else
        {
            inward.Normalize();
        }

        outward = -inward;

        windowCenter = targetWindow.barricadeAnchor != null
            ? targetWindow.barricadeAnchor.position
            : targetWindow.transform.position + Vector3.up * 2f;

        hoverPoint = windowCenter + outward * windowApproachDistance;
        entryPoint = windowCenter + outward * 0.45f;
        insidePoint = windowCenter + inward * entryDepth;

        barricade = targetWindow.barricade;
        state = FlightState.SeekWindow;
    }

    // ------------------------------------------------------------------
    // WINDOW BREACH
    // ------------------------------------------------------------------

    private void UpdateSeekWindow()
    {
        if (targetWindow == null)
        {
            enteredHouse = true;
            state = FlightState.AttackPlayer;
            return;
        }

        barricade = targetWindow.barricade;

        Vector3 destination = barricade != null ? hoverPoint : entryPoint;
        FlyTowards(destination);

        if (Vector3.Distance(transform.position, destination) > windowApproachTolerance) return;

        if (barricade != null)
        {
            state = FlightState.BreakBarricade;
            nextFireTime = Time.time + 0.25f;
        }
        else
        {
            BeginEntering();
        }
    }

    private void UpdateBreakBarricade()
    {
        if (targetWindow == null)
        {
            enteredHouse = true;
            state = FlightState.AttackPlayer;
            return;
        }

        barricade = targetWindow.barricade;

        if (barricade == null)
        {
            BeginEntering();
            return;
        }

        Vector3 aim = BarricadeAimPoint();
        FacePoint(aim);

        // Hold the firing position while the barricade is still standing.
        if (Vector3.Distance(transform.position, hoverPoint) > 0.35f)
            FlyTowards(hoverPoint, 1.25f);

        if (Time.time < nextFireTime) return;

        Shoot(aim);
        nextFireTime = Time.time + Mathf.Max(0.1f, barricadeFireInterval);
    }

    private void BeginEntering()
    {
        if (state == FlightState.EnterWindow) return;

        reachedEntry = false;
        state = FlightState.EnterWindow;
        BreakWindowGlass();
    }

    private void UpdateEnterWindow()
    {
        if (!reachedEntry)
        {
            FlyTowards(entryPoint, 1.15f);

            if (Vector3.Distance(transform.position, entryPoint) <= 0.55f)
                reachedEntry = true;

            return;
        }

        FlyTowards(insidePoint, 1.35f);

        if (Vector3.Distance(transform.position, insidePoint) > 0.7f) return;

        enteredHouse = true;
        state = FlightState.AttackPlayer;
        nextFireTime = Time.time + 0.35f;
    }

    private void BreakWindowGlass()
    {
        if (glassBroken || targetWindow == null) return;
        glassBroken = true;

        foreach (Renderer renderer in targetWindow.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null &&
                renderer.name.IndexOf("Glass", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                renderer.enabled = false;
            }
        }

        foreach (Collider collider in targetWindow.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null &&
                collider.name.IndexOf("Glass", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                collider.enabled = false;
            }
        }
    }

    private Vector3 BarricadeAimPoint()
    {
        if (barricade == null) return windowCenter;

        Collider collider = barricade.GetComponentInChildren<Collider>(true);
        if (collider != null) return collider.bounds.center;

        return barricade.transform.position;
    }

    // ------------------------------------------------------------------
    // PLAYER HUNTING
    // ------------------------------------------------------------------

    private void UpdateAttackPlayer()
    {
        if (player == null) return;

        Vector3 aimPoint = PlayerAimPoint();
        Vector3 toTarget = aimPoint - transform.position;
        float distance = toTarget.magnitude;

        Vector3 horizontal = toTarget;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude < 0.001f) horizontal = transform.forward;
        horizontal.Normalize();

        // Spread the flock around the player so several fliers do not stack
        // into the same point.
        Vector3 orbit = Quaternion.Euler(0f, orbitOffset, 0f) * horizontal;

        if (HasLineOfSight(aimPoint))
        {
            // Sit a few metres away and slightly above the player, then close
            // in whenever the player moves away.
            Vector3 hover = aimPoint - orbit * playerHoverDistance + Vector3.up * 1.15f;

            if (Vector3.Distance(transform.position, hover) > 0.6f)
                FlyTowards(hover, 1.15f);

            FacePoint(aimPoint);

            if (distance <= playerAttackRange && Time.time >= nextFireTime)
            {
                Shoot(aimPoint);
                nextFireTime = Time.time + Mathf.Max(0.2f, playerFireInterval);
                return;
            }
        }
        else
        {
            // A wall is in the way. Fly toward the player until there is a
            // clear shot instead of hovering behind the wall forever.
            FlyTowards(aimPoint, 1.25f);

            if (distance > 1.2f && Time.time >= nextFireTime)
                nextFireTime = Time.time + 0.35f;
        }
    }

    private Vector3 PlayerAimPoint()
    {
        if (player == null) return transform.position;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
            return controller.bounds.center + Vector3.up * 0.35f;

        Collider collider = player.GetComponentInChildren<Collider>();
        if (collider != null)
            return collider.bounds.center;

        return player.position + Vector3.up * 1.2f;
    }

    private bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up * 0.15f;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance < 0.2f) return true;

        RaycastHit[] hits = Physics.RaycastAll(
            origin, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;

            FlyingEnemy otherEnemy = hit.collider.GetComponentInParent<FlyingEnemy>();
            if (otherEnemy != null) continue;

            SporeHive hitHive = hit.collider.GetComponentInParent<SporeHive>();
            if (hitHive != null) continue;

            return hit.collider.GetComponentInParent<PlayerVitals>() != null;
        }

        return true;
    }

    // ------------------------------------------------------------------
    // MOVEMENT / SHOOTING
    // ------------------------------------------------------------------

    private void FlyTowards(Vector3 destination, float speedMultiplier = 1f)
    {
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            Mathf.Max(0.1f, moveSpeed) * Mathf.Max(0.1f, speedMultiplier) * Time.deltaTime);

        FacePoint(destination);
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, Mathf.Max(0.1f, turnSpeed) * Time.deltaTime);
    }

    private void ApplyBob()
    {
        if (visualRoot == null) return;

        float bob = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
        visualRoot.localPosition = visualBasePosition + Vector3.up * bob;
    }

    private void Shoot(Vector3 target)
    {
        Vector3 origin = firePoint != null
            ? firePoint.position
            : transform.position + transform.forward * 0.85f;

        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.0001f) return;

        SporeProjectile.Spawn(
            origin,
            direction.normalized,
            projectileForce,
            projectileDamage,
            barricadeDamagePerHit);
    }

    public void TakeDamage(int amount)
    {
        if (state == FlightState.Dead || amount <= 0) return;

        health -= amount;
        if (health > 0) return;

        state = FlightState.Dead;

        if (hive != null) hive.NotifyEnemyGone(this);
        Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // RUNTIME MATERIALS
    // ------------------------------------------------------------------

    private static Material CreateGlowMaterial(Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : null;
        if (material == null) return null;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        material.color = baseColor;

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission * 1.5f);
        }

        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.65f);

        return material;
    }

    private static Material CreateTrailMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return null;

        Material material = new Material(shader);
        material.color = new Color(color.r, color.g, color.b, 0.65f);
        return material;
    }
}
