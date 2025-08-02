using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
public class Bomb : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds to wait before checking for agents")]
    public float timer = 3f;

    [Header("Which polarities to destroy")]
    [Tooltip("If true, any Positive agents in range will be destroyed")]
    public bool destroyPositive = true;
    [Tooltip("If true, any Negative agents in range will be destroyed")]
    public bool destroyNegative = false;

    CircleCollider2D _col;

    void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        _col.isTrigger = true;  // ensure it's a trigger
    }

    void Start()
    {
        StartCoroutine(ExplodeAfterDelay());
    }

    IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(timer);

        // Calculate world‑space radius (accounts for any scaling)
        float worldRadius = _col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        // Find all colliders (including triggers) within that circle
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, worldRadius);

        foreach (var hit in hits)
        {
            var agent = hit.GetComponent<FlockAgent>();
            if (agent == null) continue;

            bool shouldDestroy =
                   (agent.polarity == Polarity.Positive && destroyPositive)
                || (agent.polarity == Polarity.Negative && destroyNegative);

            if (shouldDestroy)
                agent.ParentFlock.RemoveAgent(agent);
        }

        // Now remove this explosion object
        Destroy(gameObject);
    }

    // Optional: draw the circle in the editor for debugging
    void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<CircleCollider2D>();
        float worldRadius = _col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawSphere(transform.position, worldRadius);
    }
}
