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
        Configure();
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
