using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System;

/// <summary>
/// Defines event type and touch-activation. Raises OnActivated when tapped.
/// </summary>
[RequireComponent(typeof(EventUI))]
public class EventBehavior : MonoBehaviour
{
    [Header("Event Types")]
    [Tooltip("Spawns positive-polarity boids on activation.")]
    public bool spawnPositiveBoids;

    [Tooltip("Spawns negative-polarity boids on activation.")]
    public bool spawnNegativeBoids;

    [Tooltip("Triggers a bomb prefab on activation.")]
    public bool spawnBomb;

    [Tooltip("Triggers a multiplier prefab on activation.")]
    public bool spawnMultiplier;

    [Tooltip("Enables portals on activation.")]
    public bool spawnPortal;

    
    [Tooltip("World-space radius for touch activation.")]
    public float activationRadius = 1f;

    [Header("Positive Boid Parameters")]
    [Tooltip("Number of positive-polarity boids to spawn.")]
    public int positiveBoidCount = 10;

    [Header("Negative Boid Parameters")]
    [Tooltip("Number of negative-polarity boids to spawn.")]
    public int negativeBoidCount = 10;

    [Header("Bomb Parameters")]
    [Tooltip("Delay before bomb activates.")]
    public float bombTimer = 3f;

    [Header("Multiplier Parameters")]
    [Tooltip("Delay before multiplier activates.")]
    public float multiplierTimer = 3f;

    [Header("Portal Parameters")]
    [Tooltip("Tag of portal objects to enable.")]
    public string portalTag = "Portal";

    [HideInInspector] public bool spawnLeft;
    public event Action<EventBehavior> OnActivated;

    private bool _activated;

    void Awake()
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        if (_activated) return;
        foreach (var t in Touch.activeTouches)
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                TryActivate(t.screenPosition);
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryActivate(Mouse.current.position.ReadValue());
    }

    private void TryActivate(Vector2 screenPos)
    {
        Vector3 wp = Camera.main.ScreenToWorldPoint((Vector3)screenPos + Vector3.forward * 10f);
        float scaledRadius = activationRadius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        if (!_activated && Vector2.Distance((Vector2)transform.position, (Vector2)wp) <= scaledRadius)
            Activate();
    }

    private void Activate()
    {
        _activated = true;
        OnActivated?.Invoke(this);
        Destroy(gameObject);
    }
}