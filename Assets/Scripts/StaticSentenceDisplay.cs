using UnityEngine;
using TMPro;

[RequireComponent(typeof(StaticSentence))]
/// <summary>
/// Subscribes to a StaticSentence to update UI text fields and a polarity indicator bar.
/// Initialization is deferred until InitializeDisplay() is called.
/// </summary>
public class StaticSentenceDisplay : MonoBehaviour
{
    private StaticSentence sentence;
    private bool initialized = false;

    [Header("UI Text Fields")]
    [Tooltip("TMP field for the sentence text.")]
    public TMP_Text sentenceText;
    [Tooltip("TMP field for the polarity score.")]
    public TMP_Text polarityScoreText;
    [Tooltip("TMP field for the absorption points.")]
    public TMP_Text absorptionPointsText;

    [Header("Polarity Bar")]
    [Tooltip("RectTransform of the bar background (static red/green halves)")]
    public RectTransform polarityBarRect;
    [Tooltip("RectTransform of the moving indicator within the bar.")]
    public RectTransform polarityIndicator;

    private int minThreshold;
    private int maxThreshold;

    /// <summary>
    /// Must be called once, after StaticSentence.definition is set.
    /// Subscribes to events and does initial UI update.
    /// </summary>
    public void InitializeDisplay()
    {
        if (initialized) return;
        initialized = true;

        sentence = GetComponent<StaticSentence>();
        var lvls = sentence.definition.levels;
        maxThreshold = lvls[0].threshold;
        minThreshold = lvls[lvls.Count - 1].threshold;

        sentence.OnPolarityScoreChanged += UpdateUI;
        sentence.OnPolarityScoreChanged += UpdateIndicator;
        sentence.OnAbsorbed += OnAbsorbed;

        // initial UI state
        UpdateUI(sentence.PolarityScore);
        UpdateIndicator(sentence.PolarityScore);
    }

    private void UpdateUI(int polarityScore)
    {
        sentenceText.text = sentence.definition.GetTextForScore(polarityScore);
        polarityScoreText.text = polarityScore.ToString();
        absorptionPointsText.text = sentence.CalculateAbsorptionPoints().ToString();
    }

    private void UpdateIndicator(int score)
    {
        float clamped = Mathf.Clamp(score, minThreshold, maxThreshold);
        float t = (clamped - minThreshold) / (float)(maxThreshold - minThreshold);
        float x = t * polarityBarRect.rect.width;
        polarityIndicator.anchoredPosition = new Vector2(x, polarityIndicator.anchoredPosition.y);
    }

    private void OnAbsorbed(int points)
    {
        // Optional: play effects or cleanup
    }
}