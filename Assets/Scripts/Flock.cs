using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    [Header("Agent Setup")]
    public FlockAgent agentPrefab;
    public LayerMask AgentLayer;
    List<FlockAgent> agents = new List<FlockAgent>();
    public FlockBehavior behavior;

    [Header("Spawn Settings")]
    [Range(1, 500)]
    public int startingCount = 250;
    [Tooltip("Radius around this Flock’s transform to spread agents")]
    public float spawnRadius = 5f;

    [Header("Movement Settings")]
    [Range(1f, 100f)]
    public float driveFactor = 10f;
    [Range(1f, 100f)]
    public float maxSpeed = 5f;
    [Range(1f, 10f)]
    public float neighborRadius = 1.5f;
    [Range(0f, 1f)]
    public float avoidanceRadiusMultiplier = 0.5f;

    float squareMaxSpeed;
    float squareNeighborRadius;
    float squareAvoidanceRadius;
    public float SquareAvoidanceRadius => squareAvoidanceRadius;

    void Start()
    {
        squareMaxSpeed        = maxSpeed * maxSpeed;
        squareNeighborRadius  = neighborRadius * neighborRadius;
        squareAvoidanceRadius = squareNeighborRadius * avoidanceRadiusMultiplier * avoidanceRadiusMultiplier;

        // Spawn around THIS flock’s position, inside spawnRadius
        for (int i = 0; i < startingCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + (Vector3)offset;

            // optional: raycast or OverlapCircle to avoid spawning *inside* other agents
            FlockAgent newAgent = Instantiate(
                agentPrefab,
                pos,
                Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)),
                transform
            );
            newAgent.name = $"{name} Agent {i}";
            agents.Add(newAgent);
        }
    }

    void Update()
    {
        foreach (var agent in agents.ToArray())
        {
            var context = GetNearbyObjects(agent);
            Vector2 move = behavior.CalculateMove(agent, context, this) * driveFactor;
            if (move.sqrMagnitude > squareMaxSpeed)
                move = move.normalized * maxSpeed;
            agent.Move(move);
        }
    }

    List<Transform> GetNearbyObjects(FlockAgent agent)
    {
        var context = new List<Transform>();
        var cols = Physics2D.OverlapCircleAll(agent.transform.position, neighborRadius, AgentLayer);
        foreach (var c in cols)
            if (c != agent.AgentCollider)
                context.Add(c.transform);
        return context;
    }

    public void RemoveAgent(FlockAgent agent)
    {
        if (agents.Remove(agent))
            Destroy(agent.gameObject);
    }
}
