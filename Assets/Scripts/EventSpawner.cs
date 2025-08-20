using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PhaseEvent
{
    [Tooltip("Prefab with EventBehavior (+ optional EventUI)")]
    public GameObject eventPrefab;

    [Tooltip("Which phase (0-based index) to spawn this in (matches sentence order).")]
    public int phaseIndex = 0;

    [Tooltip("Seconds AFTER the phase starts to spawn this event.")]
    public float delayAfterPhaseStart = 0f;

    [Tooltip("Lifetime (seconds) before event auto-despawns if untouched.")]
    public float lifetime = 5f;
}

/// <summary>
/// Phase-based event spawner.
/// - Schedules events for the active phase (by phaseIndex) at a delay after phase start
/// - Cancels schedules on phase end or level end
/// - Joins mid-phase correctly (schedules immediately for current phase)
/// </summary>
public class EventSpawner : MonoBehaviour
{
    [Header("Phase-based events (configure here)")]
    public PhaseEvent[] phaseEvents;

    [Header("Prefabs produced by events")]
    public Flock      flockPrefab;
    public GameObject bombPrefab;
    public GameObject multiplierPrefab;

    [Header("PowerUp Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Screen-edge Offsets (0–0.5)")]
    [Tooltip("How far in from left/right (as viewport fraction)")]
    [Range(0f, 0.5f)] public float horizontalOffset = 0.1f;
    [Tooltip("How far in from top/bottom (as viewport fraction)")]
    [Range(0f, 0.5f)] public float verticalOffset   = 0.1f;

    private readonly List<Coroutine> _running = new();
    private Coroutine _subRoutine;

    void OnEnable()
    {
        _subRoutine = StartCoroutine(SubscribeWhenGMReady());
    }

    void OnDisable()
    {
        if (_subRoutine != null) StopCoroutine(_subRoutine);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseStarted -= HandlePhaseStart;
            GameManager.Instance.OnPhaseEnded   -= HandlePhaseEnd;
            GameManager.Instance.OnLevelEnded   -= HandleLevelEnd;
        }
        CancelAll();
    }

    private IEnumerator SubscribeWhenGMReady()
    {
        while (GameManager.Instance == null) yield return null;
        var gm = GameManager.Instance;

        gm.OnPhaseStarted += HandlePhaseStart;
        gm.OnPhaseEnded   += HandlePhaseEnd;
        gm.OnLevelEnded   += HandleLevelEnd;

        // If we joined mid-phase, schedule now for the current phase.
        if (gm.AllowSpawning)
            HandlePhaseStart(gm.CurrentPhaseIndex);
    }

    private void HandlePhaseStart(int phaseIndex)
    {
        CancelAll(); // fresh schedule per phase

        if (phaseEvents == null || phaseEvents.Length == 0)
        {
            if (GameManager.Instance?.verboseLogging == true)
                Debug.LogWarning("[EventSpawner] No phaseEvents configured.");
            return;
        }

        int scheduled = 0;
        foreach (var pe in phaseEvents)
        {
            if (pe == null || pe.eventPrefab == null) continue;
            if (pe.phaseIndex != phaseIndex) continue;

            _running.Add(StartCoroutine(SpawnAfterDelay(pe)));
            scheduled++;
        }

        if (GameManager.Instance?.verboseLogging == true)
            Debug.Log($"[EventSpawner] Phase {phaseIndex}: scheduled {scheduled} event(s).");
    }

    private IEnumerator SpawnAfterDelay(PhaseEvent pe)
    {
        var gm = GameManager.Instance;
        float t = 0f;

        // Wait delay while phase is allowed
        while (t < pe.delayAfterPhaseStart && gm != null && gm.AllowSpawning)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (gm == null || !gm.AllowSpawning) yield break;

        bool spawnLeft = Random.value < 0.5f;
        Vector3 pos = GetRandomLeftOrRightEdge(spawnLeft);

        var go = Instantiate(pe.eventPrefab, pos, Quaternion.identity);

        var beh = go.GetComponent<EventBehavior>();
        if (beh != null)
        {
            beh.spawnLeft = spawnLeft;

            var ui = go.GetComponent<EventUI>();
            if (ui != null) ui.Init(spawnLeft);

            Destroy(go, pe.lifetime);
            beh.OnActivated += HandleActivation;
        }
        else
        {
            if (GameManager.Instance?.verboseLogging == true)
                Debug.LogWarning("[EventSpawner] Spawned event has no EventBehavior.");
            Destroy(go, pe.lifetime);
        }
    }

    private void HandlePhaseEnd(int phaseIndex) => CancelAll();
    private void HandleLevelEnd()               => CancelAll();

    private void CancelAll()
    {
        foreach (var c in _running) if (c != null) StopCoroutine(c);
        _running.Clear();
    }

    private void HandleActivation(EventBehavior e)
    {
        Vector3 pos = e.transform.position;

        // Positive Boids
        if (e.spawnPositiveBoids && flockPrefab != null)
        {
            var flock = Instantiate(flockPrefab, pos, Quaternion.identity);
            flock.startingCount   = e.positiveBoidCount;
            flock.defaultPolarity = Polarity.Positive;
        }

        // Negative Boids
        if (e.spawnNegativeBoids && flockPrefab != null)
        {
            var flock = Instantiate(flockPrefab, pos, Quaternion.identity);
            flock.startingCount   = e.negativeBoidCount;
            flock.defaultPolarity = Polarity.Negative;
        }

        // Bomb
        if (e.spawnBomb && bombPrefab != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, spawnPoints.Length);
            var b = Instantiate(bombPrefab, spawnPoints[idx].position, Quaternion.identity);
            var bomb = b.GetComponent<Bomb>();
            if (bomb != null) bomb.timer = e.bombTimer;
        }

        // Multiplier
        if (e.spawnMultiplier && multiplierPrefab != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, spawnPoints.Length);
            var m = Instantiate(multiplierPrefab, spawnPoints[idx].position, Quaternion.identity);
            var mult = m.GetComponent<TimedPolarityMultiplier>();
            if (mult != null) mult.delay = e.multiplierTimer;
        }

        // Portal
        if (e.spawnPortal && !string.IsNullOrEmpty(e.portalTag))
        {
            foreach (var p in GameObject.FindGameObjectsWithTag(e.portalTag))
                p.SetActive(true);
        }
    }

    private Vector3 GetRandomLeftOrRightEdge(bool spawnLeft)
    {
        Camera cam = Camera.main;
        float z = -cam.transform.position.z;

        float x = spawnLeft ? horizontalOffset : 1f - horizontalOffset;
        float y = Random.Range(verticalOffset, 1f - verticalOffset);

        Vector3 viewportPos = new Vector3(x, y, z);
        Vector3 worldPos    = cam.ViewportToWorldPoint(viewportPos);
        worldPos.z = 0f;
        return worldPos;
    }
}
