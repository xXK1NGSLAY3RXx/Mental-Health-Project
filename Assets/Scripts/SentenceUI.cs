using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Sentence))]
public class SentenceUI : MonoBehaviour
{
    private Sentence sentence;
    private bool initialized = false;

    [Header("UI Text Fields")]
    public TMP_Text sentenceText;
    public TMP_Text polarityScoreText;
    public TMP_Text absorptionPointsText;

    [Header("Tier Progress (FILLED Image)")]
    public Image polarityBar;

    [Header("Timer Progress (FILLED Image)")]
    public Image timerBar;

    [Header("Threshold Progress Text")]
    public TMP_Text thresholdProgressText;

    // VFX component lives on the same object
    private SentenceVFX vfx;

    private float   initialTimer;
    private Action  _boidHitHandler;
    private Action<int> _thresholdHandler;
    private Action  _absorbRequestedHandler;

    public void InitializeDisplay()
    {
        if (initialized) return;
        initialized = true;

        sentence = GetComponent<Sentence>();
        sentence.InitFromDefinition();

        vfx = GetComponent<SentenceVFX>();
        if (vfx == null) vfx = gameObject.AddComponent<SentenceVFX>();
        if (vfx.fxRoot == null) vfx.fxRoot = GetComponent<RectTransform>();

        // Configure fills
        if (polarityBar != null)
        {
            polarityBar.type = Image.Type.Filled;
            polarityBar.fillMethod = Image.FillMethod.Horizontal;
            polarityBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            polarityBar.fillAmount = 0f;
        }
        if (timerBar != null)
        {
            timerBar.type = Image.Type.Filled;
            timerBar.fillMethod = Image.FillMethod.Radial360;
            timerBar.fillOrigin = (int)Image.Origin360.Bottom;
            timerBar.fillAmount = 0f;
        }

        // Subscribe UI updates
        sentence.OnPolarityScoreChanged += UpdateTexts;
        sentence.OnPolarityScoreChanged += UpdateTierFill;

        // Forward to VFX (store delegates for clean unsubscribe)
        _boidHitHandler         = vfx.OnBoidHit;
        _thresholdHandler       = vfx.OnThresholdReached;
        _absorbRequestedHandler = () =>
        {
            // Pass bars we want hidden during absorb; no arrays stored in VFX
            if (polarityBar && timerBar)
                vfx.PlayAbsorb(() => sentence.ConfirmAbsorb(), polarityBar.gameObject, timerBar.gameObject);
            else if (polarityBar)
                vfx.PlayAbsorb(() => sentence.ConfirmAbsorb(), polarityBar.gameObject);
            else if (timerBar)
                vfx.PlayAbsorb(() => sentence.ConfirmAbsorb(), timerBar.gameObject);
            else
                vfx.PlayAbsorb(() => sentence.ConfirmAbsorb());
        };

        sentence.OnBoidHit          += _boidHitHandler;
        sentence.OnThresholdReached += _thresholdHandler;
        sentence.OnAbsorbRequested  += _absorbRequestedHandler;

        sentence.OnAbsorbed += OnAbsorbed;

        // Initial UI state
        UpdateTexts(sentence.PolarityScore);
        UpdateTierFill(sentence.PolarityScore);

        if (timerBar != null)
            StartCoroutine(WatchTimerProgress());
    }

    void OnDisable()
    {
        if (sentence != null)
        {
            sentence.OnPolarityScoreChanged -= UpdateTexts;
            sentence.OnPolarityScoreChanged -= UpdateTierFill;

            if (_boidHitHandler != null)         sentence.OnBoidHit          -= _boidHitHandler;
            if (_thresholdHandler != null)       sentence.OnThresholdReached -= _thresholdHandler;
            if (_absorbRequestedHandler != null) sentence.OnAbsorbRequested  -= _absorbRequestedHandler;

            sentence.OnAbsorbed -= OnAbsorbed;
        }
    }

    // ---------- UI content ----------
    private void UpdateTexts(int score)
    {
        if (sentenceText)         sentenceText.text = sentence.definition?.GetTextForScore(score) ?? "";
        if (polarityScoreText)    polarityScoreText.text = score.ToString();
        if (absorptionPointsText) absorptionPointsText.text = sentence.CalculateAbsorptionPoints().ToString();

        if (thresholdProgressText)
        {
            int numer = sentence.TierIndex;                    // 0 == Default
            int denom = Mathf.Max(0, sentence.TotalTiers - 1); // non-default tiers
            thresholdProgressText.text = $"{numer}/{denom}";
        }
    }

    private void UpdateTierFill(int score)
    {
        if (!polarityBar || sentence == null) return;

        int floor  = sentence.CurrentTierFloor;
        int next   = sentence.CurrentTierCeil;
        bool atTop = sentence.AtTopTier;

        float denom    = Mathf.Max(1, next - floor);
        float progress = atTop ? 1f : Mathf.Clamp01((score - floor) / denom);

        polarityBar.fillAmount = progress;
    }

    private void OnAbsorbed(int points)
    {
        // optional: post-absorb cleanup
    }

    // ---------- Timer bar ----------
    private IEnumerator WatchTimerProgress()
    {
        while (!sentence.reachedEnd) yield return null;

        initialTimer = sentence.timer;
        if (timerBar) timerBar.fillAmount = 0f;

        while (sentence.timer > 0f)
        {
            float elapsed = initialTimer - sentence.timer;
            if (timerBar) timerBar.fillAmount = Mathf.Clamp01(elapsed / initialTimer);
            yield return null;
        }
        if (timerBar) timerBar.fillAmount = 1f;
    }
}
