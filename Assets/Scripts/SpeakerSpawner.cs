using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns a speaker dialog and its corresponding flock on a random left/right screen edge every spawnInterval.
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
    [Range(0f,1f)]
    public float negativeChance   = 0.5f;
    public int   startingCount    = 10;
    public int   agentScore       = 1;
    public float speakerLifetime  = 3f;
    public float spawnInterval    = 5f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnWave();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// Instantiates Speaker + Flock at a random left/right edge.
    /// </summary>
    void SpawnWave()
    {
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
        string text = (pool.Length>0) ? pool[Random.Range(0,pool.Length)] : string.Empty;
        ui.Init(spawnLeft, text, speakerLifetime);

        // Spawn flock
        var flock = Instantiate(flockPrefab, spawnPos, Quaternion.identity);
        flock.startingCount   = startingCount;
        flock.defaultPolarity = isNegative ? Polarity.Negative : Polarity.Positive;
        flock.polarityScore   = agentScore;
    }

    /// <summary>
    /// Converts a left/right viewport position into world space on Z=0.
    /// </summary>
    Vector3 GetRandomLeftOrRightEdge(bool spawnLeft)
    {
        float x = spawnLeft ? 0f : 1f;
        float y = Random.value;
        Camera cam = Camera.main;
        float   z   = -cam.transform.position.z;
        Vector3 vp       = new Vector3(x,y,z);
        Vector3 worldPos = cam.ViewportToWorldPoint(vp);
        worldPos.z = 0f;
        return worldPos;
    }
}
