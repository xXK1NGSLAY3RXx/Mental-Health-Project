using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PredatorSpawner : MonoBehaviour
{
    [Tooltip("Prefab for predators (Collider2D on Predator layer)")]
    public GameObject predatorPrefab;
    [Tooltip("Prefab for attractors (Collider2D on Attract layer)")]
    public GameObject attractorPrefab;

    // small history of recent touch-began events
    struct TouchInfo { public int id; public Vector2 pos; public float time; }
    private readonly List<TouchInfo> recentTouches = new List<TouchInfo>();

    // tweak these to taste:
    [Tooltip("Max time between two touches to count as a double-touch")]
    public float doubleTouchTime = 0.3f;
    [Tooltip("Max screen-space distance (px) between two touches")]
    public float doubleTouchDistance = 50f;

    // existing trackers
    private Dictionary<int, GameObject> activePredators  = new();
    private Dictionary<int, GameObject> activeAttractors = new();

    void OnEnable()  => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Update()
    {
        // --- Touch screen: predator or attractor on double-touch---
        foreach (var t in Touch.activeTouches)
        {
            int id      = t.touchId;
            Vector2 pos = t.screenPosition;

            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (TryHandleDoubleTouch(id, pos))
                        break;                  // we spawned an attractor
                    BeginPred(id, pos);        // otherwise spawn predator
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (activePredators.ContainsKey(id))
                        MovePred(id, pos);
                    if (activeAttractors.ContainsKey(id))
                        MoveAttr(id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    EndPred(id);
                    EndAttr(id);
                    break;
            }
        }

        // --- Mouse fallback unchanged ---
        const int MOUSE_ID_PRED = -1, MOUSE_ID_ATTR = -2;
        var mouse = Mouse.current;

        if (mouse.leftButton.wasPressedThisFrame)
            BeginPred(MOUSE_ID_PRED, mouse.position.ReadValue());
        if (activePredators.ContainsKey(MOUSE_ID_PRED) && mouse.leftButton.isPressed)
            MovePred(MOUSE_ID_PRED, mouse.position.ReadValue());
        if (mouse.leftButton.wasReleasedThisFrame)
            EndPred(MOUSE_ID_PRED);

        if (mouse.rightButton.wasPressedThisFrame)
            BeginAttr(MOUSE_ID_ATTR, mouse.position.ReadValue());
        if (activeAttractors.ContainsKey(MOUSE_ID_ATTR) && mouse.rightButton.isPressed)
            MoveAttr(MOUSE_ID_ATTR, mouse.position.ReadValue());
        if (mouse.rightButton.wasReleasedThisFrame)
            EndAttr(MOUSE_ID_ATTR);
    }

    /// <summary>
    /// Returns true if this touch began close enough in time & space
    /// to a previous touch-began to count as a double-touch, and
    /// in that case spawns an attractor at the midpoint.
    /// </summary>
    private bool TryHandleDoubleTouch(int id, Vector2 pos)
    {
        float now = Time.time;
        // purge too-old entries
        recentTouches.RemoveAll(t => now - t.time > doubleTouchTime);

        // see if any prior began is within distance
        foreach (var prev in recentTouches)
        {
            if ((prev.pos - pos).sqrMagnitude <= doubleTouchDistance * doubleTouchDistance)
            {
                // midpoint in screen-space
                Vector2 mid = (prev.pos + pos) * 0.5f;
                BeginAttr(id, mid);
                // clear history so a third touch won’t spawn again immediately
                recentTouches.Clear();
                return true;
            }
        }

        // otherwise record this began for the next one
        recentTouches.Add(new TouchInfo { id = id, pos = pos, time = now });
        return false;
    }

    // Predator handlers…
    void BeginPred(int id, Vector2 sp)     => activePredators[id] = Spawn(predatorPrefab, sp);
    void MovePred(int id, Vector2 sp)      => Move(existing: activePredators, id, sp);
    void EndPred(int id)                   => DestroyAndRemove(activePredators, id);

    // Attractor handlers…
    void BeginAttr(int id, Vector2 sp)     => activeAttractors[id] = Spawn(attractorPrefab, sp);
    void MoveAttr(int id, Vector2 sp)      => Move(existing: activeAttractors, id, sp);
    void EndAttr(int id)                   => DestroyAndRemove(activeAttractors, id);

    // shared spawn/move/destroy helpers:
    private GameObject Spawn(GameObject prefab, Vector2 screenPos)
    {
        var world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        return Instantiate(prefab, world, Quaternion.identity);
    }
    private void Move(Dictionary<int,GameObject> existing, int id, Vector2 sp)
    {
        if (!existing.TryGetValue(id, out var go)) return;
        var world = Camera.main.ScreenToWorldPoint(sp.WithZ(10f));
        world.z = 0f;
        go.transform.position = world;
    }
    private void DestroyAndRemove(Dictionary<int,GameObject> existing, int id)
    {
        if (!existing.TryGetValue(id, out var go)) return;
        Destroy(go);
        existing.Remove(id);
    }
}

public static class ScreenExtensions
{
    public static Vector3 WithZ(this Vector2 v, float z) => new Vector3(v.x, v.y, z);
}
