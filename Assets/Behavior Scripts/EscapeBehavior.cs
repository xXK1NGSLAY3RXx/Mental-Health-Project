using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Escape")]
public class EscapeBehavior : FlockBehavior
{
    
     [Tooltip("How far boids can sense any predator.")]
    public float detectionRadius = 5f;

    [Tooltip("Maximum strength of the flee force.")]
    public float maxFleeForce = 10f;

    [Tooltip("Only colliders on this layer will be considered predators.")]
    public LayerMask predatorLayer;

    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        // 1. Find all predators in range via layer‐filtered circle
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            agent.transform.position,
            detectionRadius,
            predatorLayer
        );
        if (hits.Length == 0) 
            return Vector2.zero;

        // 2. Build a proximity‐weighted centroid of predator positions
        Vector2 centroid = Vector2.zero;
        float totalWeight = 0f;
        foreach (var hit in hits)
        {
            Vector2 predPos = hit.transform.position;
            float dist = Vector2.Distance(agent.transform.position, predPos);
            float w = Mathf.Clamp01((detectionRadius - dist) / detectionRadius);
            centroid += predPos * w;
            totalWeight += w;
        }
        centroid /= totalWeight;

        // 3. Flee directly away from that centroid, capped at maxFleeForce
        Vector2 toCentroid = (Vector2)agent.transform.position - centroid;
        if (toCentroid.sqrMagnitude < 0.0001f) 
            return Vector2.zero;

        return toCentroid.normalized * maxFleeForce;
    }
}
