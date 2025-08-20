using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns a speaker + flock periodically, but ONLY while the current phase is active.
/// - Starts when GameManager signals phase start (after the sentence is positioned)
/// - Does an immediate spawn at phase start, then continues on an interval
/// - Stops on phase end or level end
/// - Joins mid-phase correctly (immediate spawn + loop)
/// </summary>
public class SpeakerSpawner : MonoBehaviour
{
    [Header("Prefabs & Dialogs")]
    [Tooltip("Root prefab with SpeakerUI component attached.")]
    public GameObject speakerPrefab;

    [Tooltip("Flock prefab to instantiate at the same position.")]
    public Flock flockPrefab;

    [Header("Dialog Data")]
    public string[] positiveDialogs;
    public string[] negativeDialogs;

    [Header("Spawn Configuration")]
    [Range(0f, 1f)] public float negativeChance = 0.5f;
    public int   startingCount   = 10;
    public int   agentScore      = 1;
    public float speakerLifetime = 3f;
    public float spawnInterval   = 5f;

    private Coroutine _loop;
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

        StopLoop();
    }

    private IEnumerator SubscribeWhenGMReady()
    {
        while (GameManager.Instance == null) yield return null;
        var gm = GameManager.Instance;

        gm.OnPhaseStarted += HandlePhaseStart;
        gm.OnPhaseEnded   += HandlePhaseEnd;
        gm.OnLevelEnded   += HandleLevelEnd;

        // If we appear mid-phase, treat it like a fresh start so we get an immediate spawn.
        if (gm.AllowSpawning)
            HandlePhaseStart(gm.CurrentPhaseIndex);
    }

    private void HandlePhaseStart(int phaseIndex)
    {
        if (GameManager.Instance?.verboseLogging == true)
            Debug.Log($"[SpeakerSpawner] PhaseStart {phaseIndex} – immediate spawn + loop");

        // Immediate spawn so there’s visible feedback right away.
        SpawnWave();

        // Then the periodic loop.
        if (_loop == null) _loop = StartCoroutine(SpawnLoop());
    }

    private void HandlePhaseEnd(int phaseIndex) => StopLoop();
    private void HandleLevelEnd()               => StopLoop();

    private void StopLoop()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        var gm = GameManager.Instance;
        while (gm != null && gm.AllowSpawning)
        {
            // Wait interval, then spawn (we already spawned once at phase start).
            float t = 0f;
            while (t < spawnInterval && gm.AllowSpawning)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (gm == null || !gm.AllowSpawning) break;

            SpawnWave();
        }
        _loop = null;
    }

    private void SpawnWave()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.AllowSpawning) return;

        bool isNegative = Random.value < negativeChance;
        bool spawnLeft  = Random.value < 0.5f;
        Vector3 spawnPos = GetRandomLeftOrRightEdge(spawnLeft);

        // Spawn and initialize speaker UI
        var speakerGO = Instantiate(speakerPrefab, spawnPos, Quaternion.identity);
        var ui        = speakerGO.GetComponent<SpeakerUI>();
        if (ui == null)
        {
            Debug.LogError("Speaker prefab missing SpeakerUI component!");
            Destroy(speakerGO);
            return;
        }

        // Choose dialog text
        var pool    = isNegative ? negativeDialogs : positiveDialogs;
        string text = (pool != null && pool.Length > 0) ? pool[Random.Range(0, pool.Length)] : string.Empty;
        ui.Init(spawnLeft, text, speakerLifetime);

        // Spawn flock
        var flock = Instantiate(flockPrefab, spawnPos, Quaternion.identity);
        flock.startingCount   = startingCount;
        flock.defaultPolarity = isNegative ? Polarity.Negative : Polarity.Positive;
        flock.polarityScore   = agentScore;
    }

    private Vector3 GetRandomLeftOrRightEdge(bool spawnLeft)
    {
        float x = spawnLeft ? 0f : 1f;
        float y = Random.value;
        Camera cam = Camera.main;
        float   z   = -cam.transform.position.z;
        Vector3 vp       = new Vector3(x, y, z);
        Vector3 worldPos = cam.ViewportToWorldPoint(vp);
        worldPos.z = 0f;
        return worldPos;
    }
}
