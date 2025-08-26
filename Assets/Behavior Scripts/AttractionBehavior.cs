using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Attraction")]
public class AttractionBehavior : FlockBehavior
{
    [Tooltip("How far boids can sense any attractor.")]
    public float detectionRadius = 5f;

    [Tooltip("Only colliders on this layer will be considered attractors.")]
    public LayerMask attractLayer;

    [Tooltip("Radius at which boids normally orbit.")]
    public float ringRadius = 2f;

    [Tooltip("Half-width of the dead-zone around ringRadius for blending.")]
    public float hysteresis = 0.1f;

    [Tooltip("Speed at which newcomers move to join the ring slot.")]
    public float joinSpeed = 5f;

    [Tooltip("Tangential speed when orbiting.")]
    public float orbitSpeed = 5f;

    [Tooltip("Spring strength to keep them at ringRadius.")]
    public float radialStiffness = 10f;

    [Tooltip("Smooth time for the radial spring.")]
    public float radialSmoothTime = 0.2f;

    private Vector2 radialVelocity;

    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        Vector2 pos = agent.transform.position;

        // 1) find all attractors
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, detectionRadius, attractLayer);
        if (hits.Length == 0) return Vector2.zero;

        // 2) compute centroid of attractors
        Vector2 centroid = Vector2.zero;
        foreach (var h in hits) centroid += (Vector2)h.transform.position;
        centroid /= hits.Length;

        // 3) gather current ring agents
        var allAgents = flock.transform.GetComponentsInChildren<FlockAgent>();
        List<float> angles = new List<float>();
        foreach (var a in allAgents)
        {
            Vector2 apos = a.transform.position;
            float d = Vector2.Distance(apos, centroid);
            if (Mathf.Abs(d - ringRadius) <= hysteresis)
            {
                Vector2 dir = (apos - centroid).normalized;
                angles.Add(Mathf.Atan2(dir.y, dir.x));
            }
        }

        // 4) find best gap angle
        float joinAngle;
        if (angles.Count == 0)
        {
            
            joinAngle = Random.value * Mathf.PI * 2f;
        }
        else
        {
            angles.Sort();
            float bestGap = 0f;
            joinAngle = 0f;
            for (int i = 0; i < angles.Count; i++)
            {
                float a1 = angles[i];
                float a2 = (i + 1 < angles.Count)
                    ? angles[i + 1]
                    : angles[0] + Mathf.PI * 2f;
                float gap = a2 - a1;
                if (gap > bestGap)
                {
                    bestGap = gap;
                    joinAngle = a1 + gap * 0.5f;
                }
            }
        }

        // 5) compute join position and vector
        Vector2 joinPos = centroid + new Vector2(
            Mathf.Cos(joinAngle),
            Mathf.Sin(joinAngle)
        ) * ringRadius;
        Vector2 toJoin = joinPos - pos;

        // 6) compute orbit+spring vector
        Vector2 toCenter = centroid - pos;
        float distCenter = toCenter.magnitude;
        Vector2 dirCenter = toCenter.normalized;
        Vector2 orbitVec = Vector2.zero;
        if (distCenter > 0f)
        {
            // tangential
            Vector2 tangent = new Vector2(-dirCenter.y, dirCenter.x) * orbitSpeed;
            // radial spring
            float diff = ringRadius - distCenter;  
            Vector2 targetRadial = -dirCenter * (diff * radialStiffness);
            Vector2 radial = Vector2.SmoothDamp(Vector2.zero, targetRadial, ref radialVelocity, radialSmoothTime);

            orbitVec = tangent + radial;
        }

        // 7) blend based on position relative to ring+dead-zone
        float outer = ringRadius + hysteresis;
        float inner = ringRadius - hysteresis;
        if (distCenter > outer)
        {
            // newcomer: go to slot
            return toJoin.normalized * joinSpeed;
        }
        else if (distCenter < inner)
        {
            // inside ring: orbit
            return orbitVec;
        }
        else
        {
            // between: smoothly blend join→orbit as we cross the band
            float t = Mathf.InverseLerp(inner, outer, distCenter);
            Vector2 joinVec = toJoin.normalized * joinSpeed;
            return Vector2.Lerp(orbitVec, joinVec, t);
        }
    }
}
