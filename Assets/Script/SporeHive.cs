using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A pulsing spore nest that periodically releases sphere-shaped flying
/// enemies. The nest is intentionally simple: it breathes on a sine wave and
/// pops out one flier at a time from its front opening.
/// </summary>
public class SporeHive : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Optional flying enemy prefab. Leave empty to build a sphere enemy at runtime.")]
    public GameObject flyingEnemyPrefab;

    [Tooltip("Where new fliers appear. Defaults to just in front of the hive.")]
    public Transform spawnPoint;

    [Tooltip("Seconds before the very first flier appears.")]
    public float initialDelay = 6f;

    [Tooltip("Seconds between fliers once the hive is active.")]
    public float spawnInterval = 12f;

    [Tooltip("Maximum number of living fliers from this hive.")]
    public int maxAlive = 3;

    [Tooltip("Spawn one flier immediately when the scene starts.")]
    public bool spawnOnStart = false;

    [Header("Pulse")]
    [Tooltip("The visual child that breathes. Defaults to a child named Visual.")]
    public Transform pulseTarget;

    public float pulseSpeed = 2.1f;
    [Range(0f, 0.35f)] public float pulseAmount = 0.07f;

    private readonly List<FlyingEnemy> alive = new List<FlyingEnemy>();
    private float nextSpawnTime;
    private Vector3 pulseBaseScale;
    private float pulsePhase;

    void Start()
    {
        if (pulseTarget == null)
        {
            Transform visual = transform.Find("Visual");
            pulseTarget = visual != null ? visual : transform;
        }

        pulseBaseScale = pulseTarget.localScale;
        pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        nextSpawnTime = Time.time + Mathf.Max(0.5f, initialDelay);

        if (spawnOnStart) SpawnEnemy();
    }

    void Update()
    {
        Pulse();

        if (Time.time < nextSpawnTime) return;

        PruneAlive();

        if (alive.Count < maxAlive)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + Mathf.Max(1f, spawnInterval);
        }
        else
        {
            // All pods are busy. Check again shortly in case one dies.
            nextSpawnTime = Time.time + 2f;
        }
    }

    private void Pulse()
    {
        if (pulseTarget == null) return;

        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed + pulsePhase) * pulseAmount;
        pulseTarget.localScale = pulseBaseScale * scale;
    }

    public FlyingEnemy SpawnEnemy()
    {
        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;

        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        GameObject spawned;
        if (flyingEnemyPrefab != null)
        {
            spawned = Instantiate(flyingEnemyPrefab, position, rotation);
        }
        else
        {
            // The body is deliberately built from a plain primitive sphere so
            // it reads as a simple spore creature in-game.
            spawned = new GameObject("SporeFlyingEnemy");
            spawned.transform.SetPositionAndRotation(position, rotation);

            FlyingEnemy created = spawned.AddComponent<FlyingEnemy>();
            created.hive = this;
            alive.Add(created);
            return created;
        }

        FlyingEnemy enemy = spawned.GetComponent<FlyingEnemy>();
        if (enemy == null) enemy = spawned.AddComponent<FlyingEnemy>();
        enemy.hive = this;
        alive.Add(enemy);
        return enemy;
    }

    public void NotifyEnemyGone(FlyingEnemy enemy)
    {
        if (enemy != null) alive.Remove(enemy);
    }

    private void PruneAlive()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null) alive.RemoveAt(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.45f, 1f, 0.2f, 0.7f);
        Vector3 origin = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
        Gizmos.DrawWireSphere(origin, 0.55f);
    }
}
