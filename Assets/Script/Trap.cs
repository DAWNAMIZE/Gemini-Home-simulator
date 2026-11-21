using UnityEngine;

/// <summary>
/// A placed world trap. It periodically checks for enemies inside its trigger
/// radius, damages the first one and consumes a use.
/// </summary>
public class Trap : MonoBehaviour
{
    [Header("Trap")]
    public string trapName = "Trap";
    public int damage = 45;
    public int uses = 1;
    public float triggerRadius = 1.1f;
    public float armDelay = 0.35f;
    public float retriggerDelay = 1.2f;

    private int usesLeft;
    private float armedAt;
    private float nextScanAt;

    void Start()
    {
        usesLeft = Mathf.Max(1, uses);
        armedAt = Time.time + Mathf.Max(0f, armDelay);
        nextScanAt = armedAt;
    }

    void Update()
    {
        if (usesLeft <= 0 || Time.time < armedAt || Time.time < nextScanAt) return;

        nextScanAt = Time.time + 0.15f;

        Collider[] hits = Physics.OverlapSphere(
            transform.position + Vector3.up * 0.25f, triggerRadius,
            ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null) continue;

            enemy.TakeDamage(damage);
            usesLeft--;

            ToastUI.Show(trapName + " hit for " + damage + " damage.");

            if (usesLeft <= 0)
            {
                Destroy(gameObject, 0.2f);
                return;
            }

            nextScanAt = Time.time + Mathf.Max(0.1f, retriggerDelay);
            return;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.35f, 0.15f, 0.6f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.25f, triggerRadius);
    }
}
