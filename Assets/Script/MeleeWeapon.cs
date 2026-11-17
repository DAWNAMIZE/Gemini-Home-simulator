using UnityEngine;

/// <summary>
/// Melee attack driver for weapons held in the player's hand.
/// The weapon swings through a configurable arc (45 degrees by default) and
/// deals damage in front of the camera near the apex of the swing.
/// </summary>
[DisallowMultipleComponent]
public class MeleeWeapon : MonoBehaviour
{
    [Header("Grip")]
    [Tooltip("Grip transform. Defaults to a child named 'Handle'.")]
    public Transform grip;

    [Header("Swing")]
    [Tooltip("Total swing arc in degrees.")]
    public float swingAngle = 45f;
    [Tooltip("Local axis the weapon rotates around. X gives a forward chop.")]
    public Vector3 swingAxis = Vector3.right;
    [Tooltip("Seconds for the attack portion of the swing.")]
    public float swingDuration = 0.16f;
    [Tooltip("Seconds for the weapon to settle back to its held pose.")]
    public float returnDuration = 0.30f;
    [Tooltip("Minimum seconds between attacks.")]
    public float attackCooldown = 0.45f;

    [Header("Damage")]
    public int damage = 35;
    public float hitRange = 2.4f;
    public float hitRadius = 0.55f;
    public LayerMask hitMask = ~0;

    private Quaternion restLocalRotation = Quaternion.identity;
    private bool swinging;
    private float swingElapsed;
    private bool damageApplied;
    private float nextAttackTime;
    private float lastSwingPeakAngle;

    public bool IsSwinging => swinging;

    /// <summary>Largest angle reached during the most recent swing, in degrees.</summary>
    public float LastSwingPeakAngle => lastSwingPeakAngle;

    void Awake()
    {
        ResolveGrip();
        restLocalRotation = transform.localRotation;
    }

    /// <summary>
    /// Called by WeaponInstance after the weapon has been snapped into the
    /// player's hand. The equipped rotation becomes the swing's rest pose.
    /// </summary>
    public void OnEquipped()
    {
        ResolveGrip();
        restLocalRotation = transform.localRotation;
        swinging = false;
        swingElapsed = 0f;
        damageApplied = false;
    }

    public bool TryAttack()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory != null && inventory.currentlyHeldModel != gameObject)
            return false;

        if (swinging || Time.time < nextAttackTime)
            return false;

        swinging = true;
        swingElapsed = 0f;
        damageApplied = false;
        lastSwingPeakAngle = 0f;
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, swingDuration + returnDuration);
        return true;
    }

    /// <summary>Angle offset from the held pose at a point in the swing.</summary>
    public float EvaluateSwingAngle(float elapsed)
    {
        if (!swinging) return 0f;

        if (elapsed <= swingDuration)
            return swingAngle * EaseOutCubic(elapsed / Mathf.Max(0.0001f, swingDuration));

        float back = (elapsed - swingDuration) / Mathf.Max(0.0001f, returnDuration);
        if (back >= 1f) return 0f;

        return swingAngle * (1f - EaseInOutCubic(back));
    }

    void Update()
    {
        if (!swinging) return;

        // Do not animate a weapon that was stashed or dropped mid-swing.
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory != null && inventory.currentlyHeldModel != gameObject)
        {
            ResetSwing();
            return;
        }

        swingElapsed += Time.deltaTime;
        float angle = EvaluateSwingAngle(swingElapsed);
        if (angle > lastSwingPeakAngle) lastSwingPeakAngle = angle;
        Vector3 axis = swingAxis.sqrMagnitude > 0.0001f ? swingAxis.normalized : Vector3.right;
        transform.localRotation = restLocalRotation * Quaternion.AngleAxis(angle, axis);

        if (!damageApplied && swingElapsed >= swingDuration * 0.6f)
        {
            damageApplied = true;
            ApplyHit();
        }

        if (swingElapsed >= swingDuration + returnDuration)
            ResetSwing();
    }

    private void ResetSwing()
    {
        swinging = false;
        swingElapsed = 0f;
        transform.localRotation = restLocalRotation;
    }

    private void ResolveGrip()
    {
        if (grip != null) return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == transform) continue;
            if (string.Equals(child.name, "Handle", System.StringComparison.OrdinalIgnoreCase))
            {
                grip = child;
                break;
            }
        }
    }

    private void ApplyHit()
    {
        Camera cam = Camera.main;
        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        Vector3 direction = cam != null ? cam.transform.forward : transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, hitRadius, direction,
            hitRange, hitMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform player = PlayerInventory.instance != null ? PlayerInventory.instance.transform : null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (player != null && hit.collider.transform.IsChildOf(player)) continue;

            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                return;
            }

            hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            return;
        }
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}
