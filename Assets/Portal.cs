// Portal.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Header("Link Settings")]
    [Tooltip("The paired portal to teleport to.")]
    public Portal link;

    [Tooltip("If true, this portal can teleport agents; if false, it will not.")]
    public bool enabledPortal = true;

    [Tooltip("If true, makes the portal one-way: disables the linked portal at Start.")]
    public bool oneWay = false;

    [Header("Teleport Settings")]
    [Tooltip("Cooldown (seconds) to prevent immediate back-and-forth teleport loops.")]
    public float cooldown = 0.1f;

    // Tracks agents that have just teleported to prevent recursion
    private HashSet<FlockAgent> _cooldownSet = new HashSet<FlockAgent>();

    void Awake()
    {
        // Ensure collider is trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Start()
    {
        // If one-way, disable teleport on the linked portal
        if (oneWay && link != null)
            link.enabledPortal = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Preconditions
        if (!enabledPortal || link == null) return;
        var agent = other.GetComponent<FlockAgent>();
        if (agent == null) return;

        // Prevent immediate back-and-forth
        if (_cooldownSet.Contains(agent)) return;

        // Teleport!
        agent.transform.position = link.transform.position;
        // Optionally align rotation:
        // agent.transform.rotation = link.transform.rotation;

        // Register cooldown on both portals
        _cooldownSet.Add(agent);
        link._cooldownSet.Add(agent);

        // Clear after delay
        StartCoroutine(ClearCooldown(agent));
    }

    IEnumerator ClearCooldown(FlockAgent agent)
    {
        yield return new WaitForSeconds(cooldown);
        _cooldownSet.Remove(agent);
        if (link != null)
            link._cooldownSet.Remove(agent);
    }

    // Debug: visualize portal trigger area
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col is CircleCollider2D circle)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawSphere(transform.position, radius);
        }
        else if (col is BoxCollider2D box)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }
    }
}
