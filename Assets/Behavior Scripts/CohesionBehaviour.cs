using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Cohesion")]
public class CohesionBehaviour : FlockBehavior
{
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        if (context.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 cohesionMove = Vector2.zero;
        

        foreach (Transform item in context)
        {
            
            cohesionMove += (Vector2)item.position;
           
        }

       

        cohesionMove /= context.Count; // Average position of neighbors
        cohesionMove -= (Vector2)agent.transform.position; // Offset from the agent's position
        

        return cohesionMove;
    }


}
