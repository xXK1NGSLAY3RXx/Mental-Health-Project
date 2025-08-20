using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cluster
{
    public List<FlockAgent> Members = new List<FlockAgent>();
    public Vector2 Centroid =>
        Members.Aggregate(Vector2.zero, (sum,a) => sum + (Vector2)a.transform.position)
        / Members.Count;
}
