using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns a series of sentences in phases: when one is absorbed, the next appears.
/// Each sentence spawns at <spawnPosition> then, after a delay, moves smoothly to <targetPosition>.
/// </summary>
public class SentenceSpawner : MonoBehaviour
{
    [Tooltip("Sentence prefab with StaticSentence component and StaticSentenceDisplay")]
    public GameObject sentencePrefab;

    [Tooltip("List of SentenceDefinition SOs, in spawn order.")]
    public StaticSentenceDefinition[] definitions;

    [Tooltip("World-space start position for spawned sentences.")]
    public Transform spawnPosition;

    [Tooltip("World-space target position to move sentences to (e.g. center). ")]
    public Transform targetPosition;

    [Tooltip("Delay in seconds before moving the sentence to the target.")]
    public float moveDelay = 0f;

    [Tooltip("Time (seconds) to lerp from spawn to target.")]
    public float moveDuration = 1f;

    [Tooltip("Delay in seconds between one sentence being absorbed and the next spawning.")]
    public float DelayBetweenSentences = 1f;

    public AbsorbedSentenceDisplay absorbedSentenceHUD;

    private int currentIndex = 0;
    private StaticSentence currentSentence;

    void Start()
    {
        if (absorbedSentenceHUD == null)
        {
            absorbedSentenceHUD.ClearAll();
        }
        TrySpawnNext();
    }

    void TrySpawnNext()
    {
        if (currentIndex >= definitions.Length)
            return;

        // Spawn at the spawnPosition
        var go = Instantiate(sentencePrefab, spawnPosition.position, Quaternion.identity);

        // Schedule move after delay
        StartCoroutine(DelayedMove(go.transform));

        // Configure the StaticSentence on this instance
        currentSentence = go.GetComponent<StaticSentence>();
        currentSentence.definition = definitions[currentIndex];
        currentSentence.SetLifetime(currentSentence.definition.lifetime);

        // Initialize the display now that definition is set
        var disp = go.GetComponent<StaticSentenceDisplay>();
        if (disp != null)
        {
            disp.InitializeDisplay();
        }

        // Subscribe to the absorption event
        currentSentence.OnAbsorbed += HandleAbsorbed;
    }

    private void HandleAbsorbed(int points)
    {
        // Update the HUD with the absorbed sentence
        absorbedSentenceHUD.AddSentence(
            currentSentence.definition.GetTextForScore(currentSentence.PolarityScore)
        );

        // Unsubscribe so we don't double‐call
        currentSentence.OnAbsorbed -= HandleAbsorbed;

        // Advance index, then wait before spawning the next one
        currentIndex++;
        StartCoroutine(DelayedSpawnNext());
    }

    private IEnumerator DelayedSpawnNext()
    {
        yield return new WaitForSeconds(DelayBetweenSentences);
        TrySpawnNext();
    }

    private IEnumerator DelayedMove(Transform t)
    {
        yield return new WaitForSeconds(moveDelay);
        yield return MoveToTarget(t, currentSentence);
    }

    private IEnumerator MoveToTarget(Transform t, StaticSentence sentence)
    {
        Vector3 start   = t.position;
        Vector3 end     = targetPosition.position;
        float   elapsed = 0f;

        while (elapsed < moveDuration)
        {
            t.position = Vector3.Lerp(start, end, elapsed / moveDuration);
            elapsed   += Time.deltaTime;
            yield return null;
        }

        t.position          = end;
        sentence.reachedEnd = true;
    }
}
