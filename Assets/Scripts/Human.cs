using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(LineRenderer))]
public class Human : MonoBehaviour
{
    [Tooltip("Starting health")]
    public int health = 10;

    [Tooltip("Reference to your Flock in the scene")]
    public Flock flock;

    [Tooltip("How many hitpoints per boid")]
    public int damagePerBoid = 1;

    CircleCollider2D trigger;
    LineRenderer line;

    void Start()
    {
        trigger = GetComponent<CircleCollider2D>();
        line = GetComponent<LineRenderer>();

        // Draw the circle:
        int segments = 64;
        float radius = trigger.radius * transform.localScale.x;
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float theta = (float)i / segments * 2 * Mathf.PI;
            Vector3 pos = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
            line.SetPosition(i, pos);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var agent = other.GetComponent<FlockAgent>();
        if (agent == null) return;

        // Damage the human
        health -= damagePerBoid;
        Debug.Log("Human took damage! HP now " + health);

        // Remove and destroy the boid via Flock
        flock.RemoveAgent(agent);

        // Optional: check for human death
        if (health <= 0)
        {
            Debug.Log("Human down!");
            // … handle game over …
        }
    }
}
