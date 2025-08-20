using System.Collections;
using UnityEngine;

public class SentenceSpawner : MonoBehaviour
{
    [Tooltip("Sentence prefab with StaticSentence + StaticSentenceDisplay")]
    public GameObject sentencePrefab;

    [Tooltip("SO definitions in spawn order")]
    public SentenceDefinition[] definitions;

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
    private Sentence currentSentence;

    void Start()
    {
        GameManager.Instance?.RegisterDefinitions(definitions);

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

        var go = Instantiate(sentencePrefab, spawnPosition.position, Quaternion.identity);
        currentSentence = go.GetComponent<Sentence>();
        currentSentence.definition = definitions[currentIndex];
        currentSentence.SetLifetime(currentSentence.definition.lifetime);

        var disp = go.GetComponent<SentenceUI>();
        if (disp != null) disp.InitializeDisplay();

        StartCoroutine(DelayedMove(go.transform));

        currentSentence.OnAbsorbed += HandleAbsorbed;
    }

    private IEnumerator DelayedMove(Transform t)
    {
        yield return new WaitForSeconds(moveDelay);
        yield return MoveToTarget(t, currentSentence);
    }

    private IEnumerator MoveToTarget(Transform t, Sentence sentence)
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

        // PHASE BEGINS when the sentence is positioned on screen
        GameManager.Instance.BeginPhase(currentIndex, definitions.Length);
    }

    private void HandleAbsorbed(int points)
    {
        string displayed = currentSentence.definition.GetTextForScore(currentSentence.PolarityScore);
        int maxThresh    = currentSentence.definition.levels[0].threshold;
        bool reachedTop  = currentSentence.PolarityScore >= maxThresh;

        if (absorbedSentenceHUD != null)
            absorbedSentenceHUD.AddSentence(displayed);

        GameManager.Instance?.RecordAbsorbedSentence(displayed, reachedTop);

        // Score first (optional ordering)
        GameManager.Instance.AddScore(points);

        // PHASE ENDS as soon as this sentence absorbs (will also clear boids)
        GameManager.Instance.CompletePhase(currentIndex);

        // Continue to next sentence after optional delay / feedback
        var feedbacker = GetComponent<FeedbackDialogue>();
        if (feedbacker != null)
        {
            feedbacker.TriggerFeedback(reachedTop, () =>
            {
                currentSentence.OnAbsorbed -= HandleAbsorbed;
                currentIndex++;
                StartCoroutine(DelayedSpawnNext());
            });
        }
        else
        {
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
