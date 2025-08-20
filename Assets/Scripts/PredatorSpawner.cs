using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PredatorSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab for predators (Collider2D on Predator layer)")]
    public GameObject predatorPrefab;

    [Tooltip("Prefab for attractors (Collider2D on Attract layer)")]
    public GameObject attractorPrefab;

    [Header("Double-touch settings")]
    [Tooltip("Max time between two touches to count as a double-touch")]
    public float doubleTouchTime = 0.3f;

    [Tooltip("Max screen-space distance (px) between the two touches")]
    public float doubleTouchDistance = 50f;

    // simple record of recent touch-began events
    struct TouchInfo { public int id; public Vector2 pos; public float time; }
    private readonly List<TouchInfo> recentTouches = new();

    // live objects keyed by input id
    private readonly Dictionary<int, GameObject> activePredators  = new();
    private readonly Dictionary<int, GameObject> activeAttractors = new();

    // guard so we clear exactly once when level ends
    private bool _clearedOnLevelEnd = false;

    // cached delegates to unsubscribe cleanly
    private System.Action _onLevelEndedHandler;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        // subscribe (if a persistent GameManager exists)
        if (GameManager.Instance != null)
        {
            _onLevelEndedHandler = OnLevelEnded;
            GameManager.Instance.OnLevelEnded += _onLevelEndedHandler;
        }

        _clearedOnLevelEnd = false;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();

        if (GameManager.Instance != null && _onLevelEndedHandler != null)
        {
            GameManager.Instance.OnLevelEnded -= _onLevelEndedHandler;
            _onLevelEndedHandler = null;
        }

        // Always cleanup when this component goes away
        EndAll();
    }

    void Update()
    {
        // If level has ended, stop reacting and ensure cleanup once
        var gm = GameManager.Instance;
        if (gm != null && gm.LevelHasEnded)
        {
            if (!_clearedOnLevelEnd)
            {
                EndAll();
                _clearedOnLevelEnd = true;
            }
            return;
        }

        // -------- Touch (mobile) ----------
        foreach (var t in Touch.activeTouches)
        {
            int id = t.touchId;
            Vector2 pos = t.screenPosition;

            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (TryHandleDoubleTouch(id, pos)) break; // spawned an attractor at midpoint
                    BeginPred(id, pos);                       // else spawn a predator
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (activePredators.ContainsKey(id))  Move(activePredators, id, pos);
                    if (activeAttractors.ContainsKey(id)) Move(activeAttractors, id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    End(activePredators, id);
                    End(activeAttractors, id);
                    break;
            }
        }

        // -------- Mouse (desktop) ----------
        const int MOUSE_ID_PRED = -1, MOUSE_ID_ATTR = -2;
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Left = predator drag
        if (mouse.leftButton.wasPressedThisFrame) BeginPred(MOUSE_ID_PRED, mouse.position.ReadValue());
        if (activePredators.ContainsKey(MOUSE_ID_PRED) && mouse.leftButton.isPressed)
            Move(activePredators, MOUSE_ID_PRED, mouse.position.ReadValue());
        if (mouse.leftButton.wasReleasedThisFrame) End(activePredators, MOUSE_ID_PRED);

        // Right = attractor drag
        if (mouse.rightButton.wasPressedThisFrame) BeginAttr(MOUSE_ID_ATTR, mouse.position.ReadValue());
        if (activeAttractors.ContainsKey(MOUSE_ID_ATTR) && mouse.rightButton.isPressed)
            Move(activeAttractors, MOUSE_ID_ATTR, mouse.position.ReadValue());
        if (mouse.rightButton.wasReleasedThisFrame) End(activeAttractors, MOUSE_ID_ATTR);
    }

    // ----- Double-touch detection -----
    private bool TryHandleDoubleTouch(int id, Vector2 pos)
    {
        float now = Time.time;
        // purge too-old entries
        recentTouches.RemoveAll(t => now - t.time > doubleTouchTime);

        // see if any prior began is within distance
        for (int i = 0; i < recentTouches.Count; i++)
        {
            var prev = recentTouches[i];
            if ((prev.pos - pos).sqrMagnitude <= doubleTouchDistance * doubleTouchDistance)
            {
                // midpoint in screen-space
                Vector2 mid = (prev.pos + pos) * 0.5f;
                BeginAttr(id, mid);
                recentTouches.Clear(); // prevent immediate repeats
                return true;
            }
        }

        // otherwise record this began for the next one
        recentTouches.Add(new TouchInfo { id = id, pos = pos, time = now });
        return false;
    }

    // ----- Predator helpers -----
    private void BeginPred(int id, Vector2 screenPos) => activePredators[id]  = Spawn(predatorPrefab, screenPos);
    // ----- Attractor helpers -----
    private void BeginAttr(int id, Vector2 screenPos) => activeAttractors[id] = Spawn(attractorPrefab, screenPos);

    // shared spawn/move/end
    private GameObject Spawn(GameObject prefab, Vector2 screenPos)
    {
        if (prefab == null) return null;
        var world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        return Instantiate(prefab, world, Quaternion.identity);
    }

    private void Move(Dictionary<int, GameObject> map, int id, Vector2 screenPos)
    {
        if (!map.TryGetValue(id, out var go) || go == null) return;
        var world = Camera.main.ScreenToWorldPoint(screenPos.WithZ(10f));
        world.z = 0f;
        go.transform.position = world;
    }

    private void End(Dictionary<int, GameObject> map, int id)
    {
        if (!map.TryGetValue(id, out var go)) return;
        if (go) Destroy(go);
        map.Remove(id);
    }

    private void EndAll()
    {
        foreach (var go in activePredators.Values)  if (go) Destroy(go);
        foreach (var go in activeAttractors.Values) if (go) Destroy(go);
        activePredators.Clear();
        activeAttractors.Clear();
        recentTouches.Clear();
    }

    private void OnLevelEnded()
    {
        EndAll();               // remove anything on screen
        _clearedOnLevelEnd = true;
    }
}

public static class ScreenExtensions
{
    public static Vector3 WithZ(this Vector2 v, float z) => new Vector3(v.x, v.y, z);
}
