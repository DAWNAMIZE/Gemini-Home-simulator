using UnityEngine;

/// <summary>
/// Small spore glob spat by flying enemies.
///
/// It damages the player and chips one hit off a wooden window barricade.
/// Doors are deliberately ignored: the fliers only breach windows.
/// </summary>
[DisallowMultipleComponent]
public class SporeProjectile : MonoBehaviour
{
    [Header("Damage")]
    public float damageToPlayer = 8f;
    public int barricadeHits = 1;

    [Header("Lifetime")]
    public float lifetime = 6f;

    private bool spent;

    private static Material sharedProjectileMaterial;
    private static Material sharedTrailMaterial;

    void Start()
    {
        Destroy(gameObject, Mathf.Max(0.5f, lifetime));
    }

    public static SporeProjectile Spawn(
        Vector3 origin,
        Vector3 direction,
        float force,
        float playerDamage,
        int barricadeHitCount)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "SporeProjectile";
        projectile.transform.position = origin;
        projectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        projectile.transform.localScale = Vector3.one * 0.30f;

        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = ProjectileMaterial();

        Rigidbody body = projectile.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.mass = 0.05f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        SporeProjectile spore = projectile.AddComponent<SporeProjectile>();
        spore.damageToPlayer = playerDamage;
        spore.barricadeHits = Mathf.Max(1, barricadeHitCount);

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.16f;
        trail.startWidth = 0.12f;
        trail.endWidth = 0f;
        trail.numCapVertices = 2;
        trail.sharedMaterial = TrailMaterial();
        trail.startColor = new Color(0.55f, 1f, 0.30f, 0.85f);
        trail.endColor = new Color(0.35f, 0.9f, 0.15f, 0f);

        body.AddForce(direction.normalized * Mathf.Max(1f, force), ForceMode.Impulse);
        return spore;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (spent || collision.collider == null) return;

        // Fliters do not hurt each other with their own spores.
        if (collision.collider.GetComponentInParent<FlyingEnemy>() != null) return;

        SporeHive hitHive = collision.collider.GetComponentInParent<SporeHive>();
        if (hitHive != null) return;

        WoodenBarricade barricade = collision.collider.GetComponentInParent<WoodenBarricade>();
        if (barricade != null)
        {
            barricade.Hit(Mathf.Max(1, barricadeHits), false);
            Impact(ContactPoint(collision));
            return;
        }

        PlayerVitals vitals = collision.collider.GetComponentInParent<PlayerVitals>();
        if (vitals != null)
        {
            vitals.TakeDamage(damageToPlayer);
            Impact(ContactPoint(collision));
            return;
        }

        // Walls, props and doors only absorb the glob. Doors are intentionally
        // left undamaged by this window-focused enemy.
        Impact(ContactPoint(collision));
    }

    private Vector3 ContactPoint(Collision collision)
    {
        if (collision.contactCount > 0) return collision.GetContact(0).point;
        return transform.position;
    }

    private void Impact(Vector3 point)
    {
        if (spent) return;
        spent = true;

        // A tiny green flash gives the hit some readable feedback without a
        // heavy particle prefab.
        GameObject flash = new GameObject("SporeImpact");
        flash.transform.position = point;

        Light light = flash.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.5f, 1f, 0.28f);
        light.range = 2.4f;
        light.intensity = 2.5f;
        light.shadows = LightShadows.None;

        Destroy(flash, 0.12f);
        Destroy(gameObject);
    }

    private static Material ProjectileMaterial()
    {
        if (sharedProjectileMaterial != null) return sharedProjectileMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        sharedProjectileMaterial = shader != null ? new Material(shader) : null;
        if (sharedProjectileMaterial == null) return null;

        Color baseColor = new Color(0.48f, 0.92f, 0.20f);
        if (sharedProjectileMaterial.HasProperty("_BaseColor"))
            sharedProjectileMaterial.SetColor("_BaseColor", baseColor);
        sharedProjectileMaterial.color = baseColor;

        if (sharedProjectileMaterial.HasProperty("_EmissionColor"))
        {
            sharedProjectileMaterial.EnableKeyword("_EMISSION");
            sharedProjectileMaterial.SetColor("_EmissionColor", new Color(0.6f, 1.5f, 0.25f));
        }

        return sharedProjectileMaterial;
    }

    private static Material TrailMaterial()
    {
        if (sharedTrailMaterial != null) return sharedTrailMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        sharedTrailMaterial = shader != null ? new Material(shader) : null;
        return sharedTrailMaterial;
    }
}
