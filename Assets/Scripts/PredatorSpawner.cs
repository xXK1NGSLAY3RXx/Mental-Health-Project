using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PredatorSpawner : MonoBehaviour, TouchControls.ITouchActions
{
    [Tooltip("Your Predator prefab (must have Collider2D on Predator layer)")]
    public GameObject predatorPrefab;

    TouchControls controls;
    Dictionary<int, GameObject> activePredators = new();

    void Awake()
    {
        // 1) Instantiate and hook up callbacks
        controls = new TouchControls();
        controls.Touch.SetCallbacks(this);
    }

    void OnEnable()  => controls.Enable();
    void OnDisable() => controls.Disable();

    // 2) Mouse fallback in Update
    void Update()
    {
        // if user clicks with the left mouse button…
        if (Mouse.current.leftButton.wasPressedThisFrame)
            BeginSpawn(-1, Mouse.current.position.ReadValue());
        if (activePredators.ContainsKey(-1) && Mouse.current.leftButton.isPressed)
            MovePredator(-1, Mouse.current.position.ReadValue());
        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndSpawn(-1);
    }

    // 3) New Input System touch press callback
    public void OnTouchPress(InputAction.CallbackContext ctx)
    {
        int id = ctx.control.device.deviceId;
        Vector2 pos = controls.Touch.TouchPosition.ReadValue<Vector2>();

        if (ctx.started)      BeginSpawn(id, pos);
        else if (ctx.canceled) EndSpawn(id);
    }

    // 4) New Input System touch position callback
    public void OnTouchPosition(InputAction.CallbackContext ctx)
    {
        int id = ctx.control.device.deviceId;
        if (activePredators.ContainsKey(id))
            MovePredator(id, ctx.ReadValue<Vector2>());
    }

    // --- Spawning / Moving / Despawning helpers ---
    void BeginSpawn(int pointerId, Vector2 screenPos)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint((Vector3)screenPos + Vector3.forward * 10f);
        world.z = 0;
        activePredators[pointerId] = Instantiate(predatorPrefab, world, Quaternion.identity);
    }

    void MovePredator(int pointerId, Vector2 screenPos)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint((Vector3)screenPos + Vector3.forward * 10f);
        world.z = 0;
        activePredators[pointerId].transform.position = world;
    }

    void EndSpawn(int pointerId)
    {
        Destroy(activePredators[pointerId]);
        activePredators.Remove(pointerId);
    }
}
