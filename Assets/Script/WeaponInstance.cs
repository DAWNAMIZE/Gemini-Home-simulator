using UnityEngine;

/// <summary>
/// Runtime wrapper added to every pickable weapon. Keeps the weapon's display
/// name, tells the HUD whether it is a pistol, and owns the shared pistol ammo
/// pool (15 rounds maximum).
/// </summary>
public class WeaponInstance : MonoBehaviour
{
    public const int PistolMaxAmmo = 15;

    /// <summary>Shared by all pistols so ammo survives switching weapons.</summary>
    public static int PistolAmmo = PistolMaxAmmo;

    [Header("Weapon")]
    public string weaponName = "Weapon";
    public bool isPistol;

    [Header("Pistol")]
    public Handgun handgun;

    [Header("Held Pose")]
    [Tooltip("Local position while parented to the player's HoldPosition.")]
    public Vector3 heldLocalPosition = Vector3.zero;
    [Tooltip("Local Euler angles while parented to the player's HoldPosition.")]
    public Vector3 heldLocalEuler = new Vector3(90f, 0f, 0f);
    [Tooltip("Multiplier on the weapon's original world size. 1 keeps it exactly the size it had on the floor.")]
    public Vector3 heldScaleMultiplier = Vector3.one;

    [Header("Grip / Handle")]
    [Tooltip("Optional grip transform. Auto-finds a child named 'Handle' on melee weapons.")]
    public Transform gripTransform;
    [Tooltip("Where the grip point should sit in the hand's local space.")]
    public Vector3 handGripOffset = new Vector3(0f, 0f, 0.04f);
    [Tooltip("Snap the handle/grip point into the hand instead of using heldLocalPosition.")]
    public bool alignGripToHand = true;

    [Header("Recoil Kick (Visual)")]
    [Tooltip("Local offset the weapon snaps back by when fired.")]
    public Vector3 fireKickOffset = new Vector3(0f, 0.015f, -0.09f);
    [Tooltip("How quickly the weapon settles back into the hand.")]
    public float fireKickRecovery = 16f;

    private Vector3 originalLocalScale = Vector3.one;
    private Vector3 originalWorldScale = Vector3.one;
    private Quaternion originalLocalRotation = Quaternion.identity;
    private bool capturedOriginalPose;
    private Vector3 kickOffset;

    private Vector3 gripPointLocal;
    private bool gripResolved;
    private bool gripPointResolved;
    private Vector3 resolvedHeldLocalPosition;

    private static GameObject cachedBulletPrefab;

    public static WeaponInstance Attach(GameObject weapon)
    {
        if (weapon == null) return null;

        WeaponInstance instance = weapon.GetComponent<WeaponInstance>();
        if (instance == null) instance = weapon.AddComponent<WeaponInstance>();
        instance.Configure();
        return instance;
    }

    void Awake()
    {
        CaptureOriginalPose();
        Configure();
    }

    /// <summary>
    /// Remembers how the weapon sat in the world so dropping / unequipping can
    /// restore its size and rotation exactly.
    /// </summary>
    private void CaptureOriginalPose()
    {
        if (capturedOriginalPose) return;

        originalLocalScale = transform.localScale;
        originalWorldScale = transform.lossyScale;
        originalLocalRotation = transform.localRotation;
        capturedOriginalPose = true;

        ConfigureHeldPose();
    }

    /// <summary>
    /// Sensible defaults per weapon family. The meshes are modelled with their
    /// long axis on +Y, so a 90° X rotation points blades/hammers forward.
    /// </summary>
    private void ConfigureHeldPose()
    {
        string cleanName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        string lower = cleanName.ToLowerInvariant();

        if (lower.StartsWith("pistol"))
        {
            heldLocalPosition = new Vector3(0f, -0.02f, 0.08f);
            heldLocalEuler = new Vector3(0f, 0f, 0f);
            heldScaleMultiplier = Vector3.one;
        }
        else if (lower.StartsWith("sword"))
        {
            heldLocalPosition = new Vector3(0f, -0.05f, 0.12f);
            heldLocalEuler = new Vector3(82f, 0f, 0f);
            heldScaleMultiplier = Vector3.one;
        }
        else if (lower.StartsWith("hammer"))
        {
            heldLocalPosition = new Vector3(0f, 0f, 0.12f);
            heldLocalEuler = new Vector3(88f, 0f, 0f);
            heldScaleMultiplier = Vector3.one;
        }
        else if (lower.StartsWith("ax"))
        {
            heldLocalPosition = new Vector3(0f, -0.02f, 0.12f);
            heldLocalEuler = new Vector3(84f, 0f, 0f);
            heldScaleMultiplier = Vector3.one;
        }
        else if (lower.StartsWith("dagger"))
        {
            heldLocalPosition = new Vector3(0f, -0.02f, 0.1f);
            heldLocalEuler = new Vector3(85f, 0f, 0f);
            heldScaleMultiplier = Vector3.one;
        }
    }

    void Update()
    {
        // Only animate the kick while this exact weapon is equipped in the hand.
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null || inventory.currentlyHeldModel != gameObject)
        {
            kickOffset = Vector3.zero;
            return;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition,
            resolvedHeldLocalPosition + kickOffset, Time.deltaTime * 24f);
        kickOffset = Vector3.Lerp(kickOffset, Vector3.zero,
            Time.deltaTime * fireKickRecovery);
    }

    public void ApplyFireKick()
    {
        kickOffset = Vector3.ClampMagnitude(kickOffset + fireKickOffset, 0.2f);
    }

    public void ApplyHeldPose(Transform target = null)
    {
        CaptureOriginalPose();
        ResolveGrip();

        Transform t = target != null ? target : transform;
        Quaternion heldRotation = Quaternion.Euler(heldLocalEuler);

        t.localRotation = heldRotation;
        t.localScale = CalculateHeldLocalScale(t.parent);

        if (alignGripToHand && gripTransform != null)
        {
            // Put the handle's collider center exactly on the hand instead of
            // leaving the weapon floating a fixed distance in front of it.
            Vector3 gripOffset = Vector3.Scale(gripPointLocal, t.localScale);
            resolvedHeldLocalPosition = handGripOffset - heldRotation * gripOffset;
        }
        else
        {
            resolvedHeldLocalPosition = heldLocalPosition;
        }

        t.localPosition = resolvedHeldLocalPosition;
        kickOffset = Vector3.zero;

        MeleeWeapon melee = GetComponent<MeleeWeapon>();
        if (melee != null) melee.OnEquipped();
    }

    /// <summary>
    /// Resolves the handle and the point inside it that should touch the hand.
    /// For the hammer the Handle child carries a capsule collider, so its
    /// center is the natural grip point.
    /// </summary>
    private void ResolveGrip()
    {
        if (!gripResolved)
        {
            gripResolved = true;
            if (!isPistol && gripTransform == null)
                gripTransform = FindDeepChild(transform, "Handle");
        }

        if (gripTransform == null || gripPointResolved) return;

        CapsuleCollider capsule = gripTransform.GetComponent<CapsuleCollider>();
        gripPointLocal = capsule != null
            ? transform.InverseTransformPoint(gripTransform.TransformPoint(capsule.center))
            : gripTransform.localPosition;
        gripPointResolved = true;
    }

    /// <summary>
    /// Converts the weapon's original world size into a local scale under the
    /// hand. This is what keeps a 2.2x scene pistol from shrinking to 1x when
    /// it is picked up.
    /// </summary>
    private Vector3 CalculateHeldLocalScale(Transform parent)
    {
        Vector3 targetWorld = Vector3.Scale(originalWorldScale, heldScaleMultiplier);
        if (parent == null) return targetWorld;

        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            SafeDivide(targetWorld.x, parentScale.x),
            SafeDivide(targetWorld.y, parentScale.y),
            SafeDivide(targetWorld.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }

    public void RestoreOriginalPose(Transform target = null)
    {
        Transform t = target != null ? target : transform;
        t.localRotation = originalLocalRotation;
        t.localScale = originalLocalScale;
        kickOffset = Vector3.zero;
    }

    private void Configure()
    {
        string cleanName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        weaponName = string.IsNullOrEmpty(cleanName) ? "Weapon" : cleanName;
        isPistol = weaponName.StartsWith("Pistol", System.StringComparison.OrdinalIgnoreCase);

        handgun = GetComponent<Handgun>();

        if (isPistol)
        {
            if (handgun == null) handgun = gameObject.AddComponent<Handgun>();

            if (handgun.bulletPrefab == null) handgun.bulletPrefab = ResolveBulletPrefab();
            if (handgun.muzzlePoint == null) handgun.muzzlePoint = ResolveMuzzle(transform);

            if (handgun.slideModel == null)
                handgun.slideModel = FindDeepChild(transform, "Pistol_I_BarrelGuard");

            if (handgun.bulletForce <= 0f) handgun.bulletForce = 50f;
        }

        if (GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            Bounds bounds = CalculateBounds(gameObject);
            box.center = transform.InverseTransformPoint(bounds.center);
            Vector3 scale = transform.lossyScale;
            box.size = new Vector3(
                Mathf.Abs(scale.x) > 0.0001f ? bounds.size.x / Mathf.Abs(scale.x) : bounds.size.x,
                Mathf.Abs(scale.y) > 0.0001f ? bounds.size.y / Mathf.Abs(scale.y) : bounds.size.y,
                Mathf.Abs(scale.z) > 0.0001f ? bounds.size.z / Mathf.Abs(scale.z) : bounds.size.z);
        }

        ResolveGrip();
    }

    public bool TryFire()
    {
        if (!isPistol)
        {
            ToastUI.Show(weaponName + " is a melee weapon.");
            return false;
        }

        if (PistolAmmo <= 0)
        {
            ToastUI.Show("Out of ammo - find bullets.");
            return false;
        }

        if (handgun == null)
        {
            Debug.LogWarning("WeaponInstance: " + weaponName + " has no Handgun component.");
            return false;
        }

        if (handgun.muzzlePoint == null) handgun.muzzlePoint = ResolveMuzzle(transform);
        if (handgun.bulletPrefab == null) handgun.bulletPrefab = ResolveBulletPrefab();

        handgun.FireWeapon();
        ApplyFireKick();
        PistolAmmo = Mathf.Max(0, PistolAmmo - 1);

        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();

        return true;
    }

    public static void AddAmmo(int amount)
    {
        if (amount <= 0) return;

        int before = PistolAmmo;
        PistolAmmo = Mathf.Clamp(PistolAmmo + amount, 0, PistolMaxAmmo);
        int gained = PistolAmmo - before;

        if (gained > 0)
            ToastUI.Show("+" + gained + " bullets  (" + PistolAmmo + "/" + PistolMaxAmmo + ")");
        else
            ToastUI.Show("Ammo is already full (" + PistolMaxAmmo + ")");

        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();
    }

    private static GameObject ResolveBulletPrefab()
    {
        if (cachedBulletPrefab != null) return cachedBulletPrefab;

        Handgun[] handguns = Object.FindObjectsByType<Handgun>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Handgun other in handguns)
        {
            if (other != null && other.bulletPrefab != null)
            {
                cachedBulletPrefab = other.bulletPrefab;
                return cachedBulletPrefab;
            }
        }

        cachedBulletPrefab = Resources.Load<GameObject>("Bullet_Pistol_A");
        return cachedBulletPrefab;
    }

    private static Transform ResolveMuzzle(Transform root)
    {
        Transform found = FindDeepChild(root, "MuzzlePoint");
        if (found != null) return found;

        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(root, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.04f, 0.26f);
        return muzzle.transform;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = new Bounds(root.transform.position, Vector3.one * 0.25f);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }
}
