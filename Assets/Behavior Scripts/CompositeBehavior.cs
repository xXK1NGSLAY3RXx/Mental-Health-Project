using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Compisite")]
public class CompositeBehavior : FlockBehavior
{
    public FlockBehavior[] behaviors;
    public float[] weights;
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        if (behaviors.Length != weights.Length)
        {
            Debug.LogError("Data Mismatch" + name, this);
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;

        // Calculate the weighted sum of the behaviors
        // Iterate through each behavior and apply its calculated move
        for (int i = 0; i < behaviors.Length; i++)
        {
            Vector2 partialMove = behaviors[i].CalculateMove(agent, context, flock) * weights[i];
            if (partialMove != Vector2.zero) // Check if the partial move is not zero
            {
                if (partialMove.sqrMagnitude > weights[i] * weights[i])
                {
                    partialMove.Normalize();
                    partialMove *= weights[i]; // Scale to the weight
                } 

                move += partialMove; // Accumulate the weighted move



            }


            
          
        }

        return move; 

    }
}
