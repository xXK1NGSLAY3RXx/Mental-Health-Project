using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch; 
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Spawns two kinds of interactors:
///  - Predators (left‐click or any touch) that boids flee from
///  - Attractors (right‐click only) that boids orbit/attract toward
/// </summary>
public class PredatorSpawner : MonoBehaviour
{
    [Tooltip("Prefab for predators (Collider2D on Predator layer)")]
    public GameObject predatorPrefab;
    [Tooltip("Prefab for attractors (Collider2D on Attract layer)")]
    public GameObject attractorPrefab;

    // Track touch/mouse IDs separately
    private Dictionary<int, GameObject> activePredators   = new();
    private Dictionary<int, GameObject> activeAttractors  = new();

    void OnEnable()  => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Update()
    {
        // --- Touch screen: always predator ---
        foreach (var t in Touch.activeTouches)
        {
            int id = t.touchId;
            Vector2 pos = t.screenPosition;
            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    BeginPred(id, pos);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (activePredators.ContainsKey(id))
                        MovePred(id, pos);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    EndPred(id);
                    break;
            }
        }

        // --- Mouse fallback ---
        const int MOUSE_ID_PRED    = -1;
        const int MOUSE_ID_ATTR    = -2;
        var   mouse = Mouse.current;

        // Predator = left button
        if (mouse.leftButton.wasPressedThisFrame)
            BeginPred(MOUSE_ID_PRED, mouse.position.ReadValue());
        if (activePredators.ContainsKey(MOUSE_ID_PRED) && mouse.leftButton.isPressed)
            MovePred(MOUSE_ID_PRED, mouse.position.ReadValue());
        if (mouse.leftButton.wasReleasedThisFrame)
            EndPred(MOUSE_ID_PRED);

        // Attractor = right button
        if (mouse.rightButton.wasPressedThisFrame)
            BeginAttr(MOUSE_ID_ATTR, mouse.position.ReadValue());
        if (activeAttractors.ContainsKey(MOUSE_ID_ATTR) && mouse.rightButton.isPressed)
            MoveAttr(MOUSE_ID_ATTR, mouse.position.ReadValue());
        if (mouse.rightButton.wasReleasedThisFrame)
            EndAttr(MOUSE_ID_ATTR);
    }

    // --- Predator handlers ---
    void BeginPred(int id, Vector2 screenPos)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        activePredators[id] = Instantiate(predatorPrefab, world, Quaternion.identity);
    }
    void MovePred(int id, Vector2 screenPos)
    {
        if (!activePredators.TryGetValue(id, out var go)) return;
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        go.transform.position = world;
    }
    void EndPred(int id)
    {
        if (!activePredators.TryGetValue(id, out var go)) return;
        Destroy(go);
        activePredators.Remove(id);
    }

    // --- Attractor handlers ---
    void BeginAttr(int id, Vector2 screenPos)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        activeAttractors[id] = Instantiate(attractorPrefab, world, Quaternion.identity);
    }
    void MoveAttr(int id, Vector2 screenPos)
    {
        if (!activeAttractors.TryGetValue(id, out var go)) return;
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        go.transform.position = world;
    }
    void EndAttr(int id)
    {
        if (!activeAttractors.TryGetValue(id, out var go)) return;
        Destroy(go);
        activeAttractors.Remove(id);
    }
}

public static class ScreenExtensions
{
    // helper to pack a Vector2+Z into Vector3
    public static Vector3 WithZ(this Vector2 v, float z) => new Vector3(v.x, v.y, z);
}
