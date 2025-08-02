// StayOnScreenBehavior.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behavior/Stay On Screen")]
public class StayOnScreenBehavior : FlockBehavior
{
    private Camera cam;

    [Tooltip("Viewport margin (0 to 0.5) defining how close to the edge before steering occurs.")]
    [Range(0f, 0.5f)]
    public float margin = 0.05f;

    /// <summary>
    /// Keeps agents inside the camera's viewport by steering them toward its center
    /// once they cross the specified viewport margin.
    /// </summary>
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        if (cam == null)
            cam = Camera.main;

        // Convert agent position to viewport coordinates (0-1)
        Vector3 vp = cam.WorldToViewportPoint(agent.transform.position);

        // Check if outside margins
        bool offLeft   = vp.x < margin;
        bool offRight  = vp.x > 1f - margin;
        bool offBottom = vp.y < margin;
        bool offTop    = vp.y > 1f - margin;

        if (!(offLeft || offRight || offBottom || offTop))
            return Vector2.zero;

        // World-space center of the viewport
        Vector3 worldCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, vp.z));
        Vector2 toCenter    = (worldCenter - (Vector3)agent.transform.position);

        // Compute how far past the margin we are (max of each axis)
        float dx = 0f;
        if (offLeft)   dx = margin - vp.x;
        if (offRight)  dx = vp.x - (1f - margin);
        float dy = 0f;
        if (offBottom) dy = margin - vp.y;
        if (offTop)    dy = vp.y - (1f - margin);
        float outsideAmount = Mathf.Max(dx, dy);

        // Scale the return by the squared outside amount for smoother response
        return toCenter.normalized * outsideAmount * outsideAmount;
    }
}
