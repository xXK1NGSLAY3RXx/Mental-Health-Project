using UnityEngine;
using System.Collections.Generic;

public class FlockSpawner : MonoBehaviour
{
    [Tooltip("The Flock prefab (with Flock.cs on it)")]
    public Flock flockPrefab;

    [Tooltip("How many boids per flock")]
    public int boidsPerFlock = 100;

    [Tooltip("Radius around each spawn point to spread boids")]
    public float flockSpawnRadius = 5f;

    private List<Flock> flocks = new List<Flock>();

    /// <summary>
    /// Spawns one flock at a random point along the screen border.
    /// </summary>
    public Flock SpawnSingle()
    {
        Vector3 spawnPos = GetRandomScreenBorderPosition();
        var flock = Instantiate(flockPrefab, spawnPos, Quaternion.identity);
        flock.name = $"Flock @ {spawnPos}";
        flock.startingCount = boidsPerFlock;
        flock.spawnRadius   = flockSpawnRadius;
        flocks.Add(flock);
        return flock;
    }

    /// <summary>
    /// Spawns flocks at each spawn point (random border) all at once.
    /// </summary>
    public void SpawnAll(int count)
    {
        // optional: clear existing
        foreach (var f in flocks) Destroy(f.gameObject);
        flocks.Clear();

        for (int i = 0; i < count; i++)
            SpawnSingle();
    }

    /// <summary>
    /// Picks a random point along the edge of the screen (viewport).
    /// </summary>
    Vector3 GetRandomScreenBorderPosition()
    {
        // Pick one of four edges
        float x = 0f, y = 0f;
        int side = Random.Range(0, 4); // 0=Left,1=Top,2=Right,3=Bottom

        switch (side)
        {
            case 0: // Left
                x = 0f;
                y = Random.value;
                break;
            case 1: // Top
                x = Random.value;
                y = 1f;
                break;
            case 2: // Right
                x = 1f;
                y = Random.value;
                break;
            case 3: // Bottom
                x = Random.value;
                y = 0f;
                break;
        }

        // Z needs to be distance from camera to world plane (assuming flocks at z=0)
        Camera cam = Camera.main;
        float z = -cam.transform.position.z;

        Vector3 vp = new Vector3(x, y, z);
        Vector3 world = cam.ViewportToWorldPoint(vp);
        world.z = 0f;
        return world;
    }

    // Example: spawn 3 flocks on Start
    void Start()
    {
        SpawnAll(3);
    }
}
