using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PredatorController : MonoBehaviour
{
    [Tooltip("Movement speed in units/sec")]
    public float speed = 5f;

    Rigidbody2D rb;
    Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // If you haven’t already: set the Rigidbody2D’s Body Type to Kinematic
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        // Read input each frame
        movement.x = Input.GetKey(KeyCode.D) ?  1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
        movement.y = Input.GetKey(KeyCode.W) ?  1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        movement = movement.normalized;  // so diagonal isn’t faster
    }

    void FixedUpdate()
    {
        // Move in physics step
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }
}
