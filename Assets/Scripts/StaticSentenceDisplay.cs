using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(StaticSentence))]
/// <summary>
/// Subscribes to a StaticSentence to update UI text fields, a polarity indicator bar,
/// and a timer-based progress bar fill.
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

    [Header("Timer Progress Bar")]
    [Tooltip("Image component whose fillAmount represents timer progress.")]
    public Image progressBar;

    private int minThreshold;
    private int maxThreshold;
    private float initialTimer;

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

        // Subscribe to polarity events
        sentence.OnPolarityScoreChanged += UpdateUI;
        sentence.OnPolarityScoreChanged += UpdateIndicator;
        sentence.OnAbsorbed += OnAbsorbed;

        // Initial UI state
        UpdateUI(sentence.PolarityScore);
        UpdateIndicator(sentence.PolarityScore);

        // If a progress bar is assigned, start watching the timer
        if (progressBar != null)
            StartCoroutine(WatchTimerProgress());
    }

    private void UpdateUI(int polarityScore)
    {
        if (sentenceText) sentenceText.text = sentence.definition.GetTextForScore(polarityScore);
        if (polarityScoreText) polarityScoreText.text = polarityScore.ToString();
        if (absorptionPointsText) absorptionPointsText.text = sentence.CalculateAbsorptionPoints().ToString();
    }

    private void UpdateIndicator(int score)
    {
        float clamped = Mathf.Clamp(score, minThreshold, maxThreshold);
        float t = (clamped - minThreshold) / (float)(maxThreshold - minThreshold);
        float x = t * polarityBarRect.rect.width;
        if (polarityIndicator)
            polarityIndicator.anchoredPosition = new Vector2(x, polarityIndicator.anchoredPosition.y);
    }

    private void OnAbsorbed(int points)
    {
        // Optional: play effects or cleanup
    }

    /// <summary>
    /// Coroutine that watches for the sentence's timer start and updates the progressBar.fillAmount.
    /// When timer begins (reachedEnd==true), captures initial timer value.
    /// Fills from 0 (start) to 1 (when timer reaches zero).
    /// </summary>
    private IEnumerator WatchTimerProgress()
    {
        // Wait until the StaticSentence signals that its timer should start
        while (!sentence.reachedEnd)
            yield return null;

        // Capture the starting timer
        initialTimer = sentence.timer;
        progressBar.fillAmount = 0f;

        // Update until timer runs out
        while (sentence.timer > 0f)
        {
            float elapsed = initialTimer - sentence.timer;
            progressBar.fillAmount = Mathf.Clamp01(elapsed / initialTimer);
            yield return null;
        }

        // Ensure fully filled
        progressBar.fillAmount = 1f;
    }
}
