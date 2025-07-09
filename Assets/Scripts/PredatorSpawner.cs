using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch; 
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PredatorSpawner : MonoBehaviour
{
    [Tooltip("Your Predator prefab (must have Collider2D on Predator layer)")]
    public GameObject predatorPrefab;

    // Track each finger by its unique touchId
    private Dictionary<int, GameObject> activePredators = new();

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // --- Touch screen multitouch via Enhanced Touch ---
        foreach (var t in Touch.activeTouches)
        {
            int id = t.touchId;
            Vector2 pos = t.screenPosition;

            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    BeginSpawn(id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (activePredators.ContainsKey(id))
                        MovePredator(id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    EndSpawn(id);
                    break;
            }
        }

        // --- Mouse fallback for Editor / Standalone ---
        const int MOUSE_ID = -1;
        var mouse = Mouse.current;
        if (mouse.leftButton.wasPressedThisFrame)
            BeginSpawn(MOUSE_ID, mouse.position.ReadValue());
        if (activePredators.ContainsKey(MOUSE_ID) && mouse.leftButton.isPressed)
            MovePredator(MOUSE_ID, mouse.position.ReadValue());
        if (mouse.leftButton.wasReleasedThisFrame)
            EndSpawn(MOUSE_ID);
    }

    void BeginSpawn(int id, Vector2 screenPos)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint((Vector3)screenPos + Vector3.forward * 10f);
        world.z = 0f;
        activePredators[id] = Instantiate(predatorPrefab, world, Quaternion.identity);
    }

    void MovePredator(int id, Vector2 screenPos)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint((Vector3)screenPos + Vector3.forward * 10f);
        world.z = 0f;
        activePredators[id].transform.position = world;
    }

    void EndSpawn(int id)
    {
        if (!activePredators.ContainsKey(id)) return;
        Destroy(activePredators[id]);
        activePredators.Remove(id);
    }
}
