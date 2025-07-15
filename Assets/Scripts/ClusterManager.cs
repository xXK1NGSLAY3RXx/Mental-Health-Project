using System.Collections.Generic;
using UnityEngine;

public class ClusterManager : MonoBehaviour
{
    [Tooltip("Distance within which boids are connected")]
    public float clusterRadius = 1.5f;

    [Tooltip("Only colliders on this layer count as boids")]
    public LayerMask boidLayer;

    public List<Cluster> BuildClusters()
    {
        var agents = FlockAgent.AllAgents;
        var unvisited = new HashSet<FlockAgent>(agents);
        var clusters = new List<Cluster>();

        while (unvisited.Count > 0)
        {
            var seed = System.Linq.Enumerable.First(unvisited);
            var queue = new Queue<FlockAgent>();
            var cluster = new Cluster();

            queue.Enqueue(seed);
            unvisited.Remove(seed);

            while (queue.Count > 0)
            {
                var a = queue.Dequeue();
                cluster.Members.Add(a);

                // Physics-based neighbor lookup
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    a.transform.position,
                    clusterRadius,
                    boidLayer
                );
                foreach (var hit in hits)
                {
                    var b = hit.GetComponent<FlockAgent>();
                    if (b != null && unvisited.Contains(b))
                    {
                        queue.Enqueue(b);
                        unvisited.Remove(b);
                    }
                }
            }
            clusters.Add(cluster);
        }
        return clusters;
    }
}
