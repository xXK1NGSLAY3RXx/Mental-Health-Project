using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Tooltip("Hook this up to your ScoreText TMP component")]
    public TMP_Text scoreText;

    [Tooltip("Reference your SentenceManager here")]
    public SentenceManager sentenceMgr;

    void Awake()
    {
        // initialize UI
        scoreText.text = "Score: 0";
        // listen for absorbs
        sentenceMgr.onSentenceAbsorbed += OnSentenceAbsorbed;
    }

    void OnDestroy()
    {
        sentenceMgr.onSentenceAbsorbed -= OnSentenceAbsorbed;
    }

    void OnSentenceAbsorbed(SentenceInstance si)
    {
        // pull the updated totalScore
        scoreText.text = $"Score: {sentenceMgr.totalScore}";
    }
}
