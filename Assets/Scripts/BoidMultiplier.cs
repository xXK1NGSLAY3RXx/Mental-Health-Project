using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
public class TimedPolarityMultiplier : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds to wait before multiplying agents")]
    public float delay = 3f;

    [Header("Which polarities to multiply")]
    public bool multiplyPositive = true;
    public bool multiplyNegative = false;

    [Header("Multiplication Settings")]
    [Tooltip("How many new agents to spawn per matching agent in range")]
    public int spawnPerAgent = 2;

    private CircleCollider2D _col;

    void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        _col.isTrigger = true;
    }

    void Start()
    {
        StartCoroutine(MultiplyAfterDelay());
    }

    IEnumerator MultiplyAfterDelay()
    {
        yield return new WaitForSeconds(delay);

        float worldRadius = _col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, worldRadius);

        foreach (var hit in hits)
        {
            var agent = hit.GetComponent<FlockAgent>();
            if (agent == null) continue;

            bool shouldMultiply = (agent.polarity == Polarity.Positive && multiplyPositive)
                                || (agent.polarity == Polarity.Negative && multiplyNegative);

            if (shouldMultiply)
            {
                agent.ParentFlock.SpawnAgentsWithPolarity(
                    agent.polarity,
                    transform.position,
                    spawnPerAgent
                );
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<CircleCollider2D>();
        float worldRadius = _col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawSphere(transform.position, worldRadius);
    }
}
