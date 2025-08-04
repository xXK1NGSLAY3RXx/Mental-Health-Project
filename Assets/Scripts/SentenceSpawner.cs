// SentenceSpawner.cs
using System.Collections;
using UnityEngine;

public class SentenceSpawner : MonoBehaviour
{
    [Tooltip("Sentence prefab with StaticSentence + StaticSentenceDisplay")]
    public GameObject sentencePrefab;

    [Tooltip("SO definitions in spawn order")]
    public StaticSentenceDefinition[] definitions;

    [Tooltip("Where new sentences appear")]
    public Transform spawnPosition;

    [Tooltip("Where they lerp to")]
    public Transform targetPosition;

    [Tooltip("Delay before moving into view")]
    public float moveDelay = 0f;

    [Tooltip("How long to lerp")]
    public float moveDuration = 1f;

    [Tooltip("Delay between one absorb and next spawn")]
    public float sentenceDelay = 1f;

    public AbsorbedSentenceDisplay absorbedSentenceHUD;

    private int currentIndex = 0;
    private StaticSentence currentSentence;

    void Start()
    {
        if (absorbedSentenceHUD != null)
            absorbedSentenceHUD.ClearAll();

        TrySpawnNext();
    }

    void TrySpawnNext()
    {
        if (currentIndex >= definitions.Length)
        {
            GameManager.Instance.EndLevel();
            return;
        }

        // spawn
        var go = Instantiate(sentencePrefab, spawnPosition.position, Quaternion.identity);
        currentSentence = go.GetComponent<StaticSentence>();
        currentSentence.definition = definitions[currentIndex];
        currentSentence.SetLifetime(currentSentence.definition.lifetime);

        // display text
        var disp = go.GetComponent<StaticSentenceDisplay>();
        if (disp != null) disp.InitializeDisplay();

        // move after a moment
        StartCoroutine(DelayedMove(go.transform));

        // watch for absorb
        currentSentence.OnAbsorbed += HandleAbsorbed;
    }

    private IEnumerator DelayedMove(Transform t)
    {
        yield return new WaitForSeconds(moveDelay);
        yield return MoveToTarget(t, currentSentence);
    }

    private IEnumerator MoveToTarget(Transform t, StaticSentence sentence)
    {
        Vector3 start = t.position;
        Vector3 end   = targetPosition.position;
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

    private void HandleAbsorbed(int points)
   {
     // 1) Update HUD
     if (absorbedSentenceHUD != null)
     {
        absorbedSentenceHUD.AddSentence(
            currentSentence.definition.GetTextForScore(currentSentence.PolarityScore)
        );
     }

     // 2) Add to player score
     GameManager.Instance.AddScore(points);

     // 3) Trigger feedback dialogue if available
       var feedbacker = GetComponent<FeedbackDialogue>();
     if (feedbacker != null)
     {
        int maxThresh = currentSentence.definition.levels[0].threshold;
        bool isGood   = currentSentence.PolarityScore >= maxThresh;

        feedbacker.TriggerFeedback(isGood, () =>
        {
            // 4) After dialogue, unsubscribe and spawn next
            currentSentence.OnAbsorbed -= HandleAbsorbed;
            currentIndex++;
            StartCoroutine(DelayedSpawnNext());
        });
     }
     else
     {
        // No feedbacker: proceed immediately
        currentSentence.OnAbsorbed -= HandleAbsorbed;
        currentIndex++;
        StartCoroutine(DelayedSpawnNext());
     }
    }
    private IEnumerator DelayedSpawnNext()
    {
        yield return new WaitForSeconds(sentenceDelay);
        TrySpawnNext();
    }
}
